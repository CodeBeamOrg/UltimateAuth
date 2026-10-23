using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabDashboardService(SandboxService sandboxService)
{
    public async Task<LabDashboardSnapshot?> GetAsync(UserKey userKey, CancellationToken ct = default)
    {
        var sandbox = await sandboxService.GetByUserAsync(userKey, ct);

        if (sandbox is null)
            return null;

        var members = await sandboxService.GetMembersAsync(sandbox.Id, ct);

        var maxUsers = sandbox.Settings.MaxUsers;
        var memberCount = members.Count;

        return new LabDashboardSnapshot(
            SandboxId: sandbox.Id,
            SandboxName: sandbox.Name,
            MaxUsers: maxUsers,
            MemberCount: memberCount,
            RemainingCapacity: Math.Max(0, maxUsers - memberCount),
            IsOwner: sandbox.OwnerUserKey == userKey);
    }
}
