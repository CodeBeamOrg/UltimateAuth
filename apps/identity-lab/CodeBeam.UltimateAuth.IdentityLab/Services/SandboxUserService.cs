using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class SandboxUserService(SandboxService sandboxService, SandboxAccessService accessService, IUAuthClient uAuthClient, IRoleStoreFactory roleStoreFactory, IUserRoleStoreFactory userRoleStoreFactory)
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
        var users = new List<SandboxUserItem>();

        foreach (var member in members)
        {
            var result = await uAuthClient.Users.GetUserAsync(member.UserKey);

            if (!result.IsSuccess || result.Value is null)
                throw new InvalidOperationException($"Unable to load user {member.UserKey.Value}.");

            var user = result.Value;

            users.Add(new SandboxUserItem(
                UserKey: member.UserKey,
                DisplayName: user.DisplayName
                    ?? user.UserName
                    ?? "Unknown user",
                Username: user.UserName ?? "",
                Email: user.PrimaryEmail ?? "",
                IsOwner: member.Role == SandboxMemberRole.Owner,
                Status: user.Status,
                CreatedAt: user.CreatedAt ?? member.JoinedAt
            ));
        }

        return new SandboxUsersSnapshot(sandbox.Id, sandbox.Settings.MaxUsers, users);
    }

    public async Task<CreateUserDialogResult> CreateAsync(UserKey actorUserKey, CreateUserRequest request, CancellationToken ct = default)
    {
        var sandbox = await sandboxService.GetByUserAsync(actorUserKey, ct);

        if (sandbox is null)
            return CreateUserDialogResult.Failure("Sandbox not found.");

        var access = await accessService.CanManageAsync(actorUserKey, sandbox.Id, ct);

        if (!access.Allowed)
            return CreateUserDialogResult.Failure("You are not allowed to manage this sandbox.");

        var remaining = await sandboxService.GetRemainingCapacityAsync(sandbox.Id, ct);

        if (remaining <= 0)
            return CreateUserDialogResult.Failure("Sandbox user limit reached.");

        var result = await uAuthClient.Users.CreateAsAdminAsync(request);

        if (!result.IsSuccess || result.Value is null || result.Value.UserKey is null)
            return CreateUserDialogResult.Failure(result.ErrorText ?? "User creation failed.");

        var newUserKey = UserKey.FromString(result.Value.UserKey);

        try
        {
            await AssignUserRoleAsync(newUserKey, ct);
            await sandboxService.AddMemberAsync(actorUserKey, newUserKey, ct);
        }
        catch (Exception)
        {
            return CreateUserDialogResult.Failure(
                "User was created, but sandbox membership failed. " +
                "Manual repair is required.");
        }

        return CreateUserDialogResult.Success();
    }

    private async Task AssignUserRoleAsync(UserKey userKey, CancellationToken ct)
    {
        var tenant = TenantKeys.Single;

        var roleStore = roleStoreFactory.Create(tenant);
        var userRoleStore = userRoleStoreFactory.Create(tenant);

        var role = await roleStore.GetByNameAsync("USER", ct);

        if (role is null)
            throw new InvalidOperationException("Identity Lab User role is not configured.");

        var assignments = await userRoleStore.GetAssignmentsAsync(userKey, ct);

        if (assignments.Any(x => x.RoleId == role.Id))
            return;

        await userRoleStore.AssignAsync(userKey, role.Id, DateTimeOffset.UtcNow, ct);
    }
}
