using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Authorization;

public sealed class LabAdminGuard(IRoleStoreFactory roleStoreFactory, IUserRoleStoreFactory userRoleStoreFactory) : ILabAdminGuard
{
    public async Task<bool> IsAdminAsync(AccessContext context, CancellationToken ct = default)
    {
        if (!context.IsAuthenticated ||
            context.IsSystemActor ||
            context.ActorUserKey is null ||
            context.ActorTenant.IsSystem ||
            context.ActorTenant.IsUnresolved ||
            context.IsCrossTenant)
        {
            return false;
        }

        var roleStore = roleStoreFactory.Create(context.ActorTenant);

        var adminRole = await roleStore.GetByNameAsync(LabRoles.Admin.ToUpperInvariant(), ct);

        if (adminRole is null)
            return false;

        var userRoleStore = userRoleStoreFactory.Create(context.ActorTenant);

        var assignments = await userRoleStore.GetAssignmentsAsync(context.ActorUserKey.Value, ct);

        return assignments.Any(x => x.RoleId == adminRole.Id);
    }

    public async Task EnsureAdminAsync(AccessContext context, CancellationToken ct = default)
    {
        if (!await IsAdminAsync(context, ct))
        {
            throw new UnauthorizedAccessException("LabAdmin permission is required.");
        }
    }
}
