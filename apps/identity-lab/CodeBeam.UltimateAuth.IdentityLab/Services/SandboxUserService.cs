using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class SandboxUserService(SandboxService sandboxService, IUAuthClient uAuthClient, IDbContextFactory<LabDbContext> dbFactory, SandboxAccessService accessService, LabUserProvisioningService provisioningService, 
                    IUserSummaryQueryStoreFactory userSummaryQueryStoreFactory, ActivityLogService activityLogService, IRoleStoreFactory roleStoreFactory, IUserRoleStoreFactory userRoleStoreFactory, ILogger<SandboxUserService> logger)
{
    public async Task<SandboxUsersSnapshot?> GetUsersAsync(UserKey actorUserKey, CancellationToken ct = default)
    {
        var sandbox = await sandboxService.GetByUserAsync(actorUserKey, ct);

        if (sandbox is null)
            return null;

        var access = await accessService.CanReadAsync(actorUserKey, sandbox.Id, ct);

        if (!access.Allowed)
            throw new UnauthorizedAccessException();

        var members = await sandboxService.GetMembersAsync(sandbox.Id, ct);

        if (members.Count == 0)
        {
            return new SandboxUsersSnapshot(sandbox.Id, sandbox.Settings.MaxUsers, Array.Empty<SandboxUserItem>());
        }

        var keys = members.Select(x => x.UserKey).Distinct().ToArray();

        var store = userSummaryQueryStoreFactory.Create(TenantKeys.Single);

        var summaries = await store.GetByKeysAsync(keys, ct);

        var userMap = summaries.ToDictionary(x => x.UserKey);

        var users = new List<SandboxUserItem>(members.Count);

        foreach (var member in members)
        {
            if (!userMap.TryGetValue(member.UserKey, out var user))
            {
                throw new InvalidOperationException($"Unable to load user {member.UserKey.Value}.");
            }

            users.Add(new SandboxUserItem(
                UserKey: member.UserKey,
                DisplayName: user.DisplayName
                    ?? user.UserName
                    ?? "Unknown user",
                Username: user.UserName ?? "",
                Email: user.PrimaryEmail ?? "",
                IsOwner: member.Role == SandboxMemberRole.Owner,
                Status: user.Status,
                CreatedAt: user.CreatedAt
            ));
        }

        return new SandboxUsersSnapshot(sandbox.Id, sandbox.Settings.MaxUsers, users);
    }

    public async Task<CreateUserDialogResult> CreateAsync(UserKey actorUserKey, CreateUserRequest request, CancellationToken ct = default)
    {
        var sandbox = await sandboxService.GetByUserAsync(actorUserKey, ct);

        if (sandbox is null)
            return CreateUserDialogResult.Failure("Sandbox not found.");

        try
        {
            var result = await provisioningService.CreateMemberAsync(
                actorUserKey,
                request,
                ct);

            if (!result.IsSuccess ||
                string.IsNullOrWhiteSpace(result.UserKey))
            {
                return CreateUserDialogResult.Failure(
                    result.FailureReason ?? "User creation failed.");
            }

            await activityLogService.TryRecordAsync(
                sandbox.Id,
                actorUserKey.Value,
                ActivityEvents.UserCreated,
                result.UserKey,
                ct);

            return CreateUserDialogResult.Success();
        }
        catch (LabPartialProvisioningException)
        {
            return CreateUserDialogResult.Failure(
                "The account was created, but sandbox setup could not be completed. " +
                "Please contact the administrator for repair.");
        }
        catch (UnauthorizedAccessException)
        {
            return CreateUserDialogResult.Failure(
                "You are not allowed to manage this sandbox.");
        }
        catch (UAuthValidationException ex)
        {
            return CreateUserDialogResult.Failure(
                FormatValidationError(ex.Message));
        }
        catch (InvalidOperationException ex)
            when (ex.Message == "Sandbox user limit reached.")
        {
            return CreateUserDialogResult.Failure(
                "Sandbox user limit reached. Remove a member before adding another.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to create sandbox user.");

            return CreateUserDialogResult.Failure(
                "Unable to create the user. Please try again.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteAsync(
    UserKey actorUserKey,
    UserKey targetUserKey,
    CancellationToken ct = default)
    {
        if (actorUserKey == targetUserKey)
            return (false, "You cannot delete your own account.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var sandbox = await db.Sandboxes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OwnerUserKey == actorUserKey &&
                     x.Status == SandboxStatus.Active,
                ct);

        if (sandbox is null)
            return (false, "You are not the owner of an active sandbox.");

        var owner = await db.SandboxMembers
            .AsNoTracking()
            .AnyAsync(x =>
                x.SandboxId == sandbox.Id &&
                x.UserKey == actorUserKey &&
                x.Role == SandboxMemberRole.Owner, ct);

        if (!owner)
            return (false, "Owner membership is required.");

        var member = await db.SandboxMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.SandboxId == sandbox.Id &&
                x.UserKey == targetUserKey, ct);

        if (member is null || member.Role != SandboxMemberRole.Member)
            return (false, "The target is not a regular member of your sandbox.");

        // Prevent deleting an account with elevated global roles.
        var roleStore = roleStoreFactory.Create(TenantKeys.Single);
        var userRoleStore = userRoleStoreFactory.Create(TenantKeys.Single);
        var assignments = await userRoleStore.GetAssignmentsAsync(targetUserKey, ct);

        var ownerRole = await roleStore.GetByNameAsync(LabRoles.Owner.ToUpperInvariant(), ct);
        var adminRole = await roleStore.GetByNameAsync(LabRoles.Admin.ToUpperInvariant(), ct);

        if (ownerRole is null || adminRole is null)
            return (false, "Required roles are not configured.");

        if (assignments.Any(x =>
            x.RoleId == ownerRole.Id || x.RoleId == adminRole.Id))
        {
            return (false, "Privileged accounts cannot be deleted.");
        }

        try
        {
            var result = await uAuthClient.Users.DeleteUserAsync(
                targetUserKey,
                new DeleteUserRequest() { Mode = DeleteMode.Hard });

            if (!result.IsSuccess)
                return (false, result.ErrorText ?? "User deletion failed.");

            // Remove sandbox membership only after framework deletion succeeds.
            var currentMember = await db.SandboxMembers
                .SingleOrDefaultAsync(x =>
                    x.SandboxId == sandbox.Id &&
                    x.UserKey == targetUserKey, ct);

            if (currentMember is not null)
            {
                db.SandboxMembers.Remove(currentMember);
                await db.SaveChangesAsync(ct);
            }

            await activityLogService.TryRecordAsync(
                sandbox.Id,
                actorUserKey.Value,
                ActivityEvents.UserDeleted,
                targetUserKey.Value,
                ct);

            return (true, "User permanently deleted.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to delete sandbox user {TargetUserKey} by {ActorUserKey}",
                targetUserKey.Value,
                actorUserKey.Value);

            return (false, "User deletion failed. Check server logs before retrying.");
        }
    }

    private static string FormatValidationError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Invalid user information.";

        if (message.Contains("username_unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return "This username is already taken.";
        }

        if (message.Contains("email_unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return "This email address is already registered.";
        }

        if (message.Contains("phone_unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return "This phone number is already registered.";
        }

        return "Please check the user information and try again.";
    }
}
