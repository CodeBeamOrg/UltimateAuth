using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabDashboardService(SandboxService sandboxService, IUAuthClient uAuthClient)
{
    public async Task<LabDashboardSnapshot?> GetAsync(UserKey userKey, CancellationToken ct = default)
    {
        var sandbox = await sandboxService.GetByUserAsync(userKey, ct);

        if (sandbox is null)
            return null;

        var members = await sandboxService.GetMembersAsync(sandbox.Id, ct);
        var maxUsers = Math.Min(sandbox.Settings.MaxUsers, SandboxLimits.AbsoluteMaxUsers);
        var memberCount = members.Count;

        string? displayName = null;
        string? username = null;
        string? email = null;
        string? accountStatus = null;

        int? chainCount = null;
        int? roleCount = null;

        try
        {
            var result = await uAuthClient.Users.GetMeAsync();

            if (result.IsSuccess && result.Value is not null)
            {
                var profile = result.Value;

                displayName = profile.DisplayName;
                username = profile.UserName;
                email = profile.PrimaryEmail;
                accountStatus = profile.Status.ToString();
            }
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Profile information is optional for the overview.
        }

        try
        {
            var result = await uAuthClient.Sessions.GetMyChainsAsync(
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 1
                });

            if (result.IsSuccess && result.Value is not null)
            {
                chainCount = result.Value.TotalCount;
            }
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Chain information is optional for the overview.
        }

        try
        {
            var result = await uAuthClient.Authorization.GetMyRolesAsync(
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 1
                });

            if (result.IsSuccess && result.Value is not null)
            {
                roleCount = result.Value.Roles.TotalCount;
            }
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Role information is optional for the overview.
        }

        return new LabDashboardSnapshot(
            SandboxId: sandbox.Id,
            SandboxName: sandbox.Name,
            MaxUsers: maxUsers,
            MemberCount: memberCount,
            RemainingCapacity: Math.Max(0, maxUsers - memberCount),
            IsOwner: sandbox.OwnerUserKey == userKey,

            DisplayName: displayName,
            Username: username,
            Email: email,
            AccountStatus: accountStatus,

            ChainCount: chainCount,
            RoleCount: roleCount);
    }
}
