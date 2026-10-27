using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class SandboxUserService(SandboxService sandboxService, SandboxAccessService accessService, LabUserProvisioningService provisioningService, 
                    IUserSummaryQueryStoreFactory userSummaryQueryStoreFactory, ActivityLogService activityLogService)
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

    public async Task<CreateUserDialogResult> CreateAsync(
    UserKey actorUserKey,
    CreateUserRequest request,
    CancellationToken ct = default)
    {
        var sandbox = await sandboxService.GetByUserAsync(
            actorUserKey, ct);

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
        catch (UnauthorizedAccessException)
        {
            return CreateUserDialogResult.Failure(
                "You are not allowed to manage this sandbox.");
        }
        catch (Exception)
        {
            return CreateUserDialogResult.Failure(
                "User provisioning failed. The account may require repair.");
        }
    }
}
