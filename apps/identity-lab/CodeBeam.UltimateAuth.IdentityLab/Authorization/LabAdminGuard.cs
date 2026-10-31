using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;

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

        var tenant = context.ActorTenant;
        var roleStore = roleStoreFactory.Create(tenant);

        var roles = await roleStore.GetByIdsAsync(
            [LabRoleIds.Admin], ct);

        var adminRole = roles.FirstOrDefault(x =>
            x.Id == LabRoleIds.Admin &&
            x.Tenant == tenant &&
            !x.IsDeleted);

        if (adminRole is null)
            return false;

        var userRoleStore = userRoleStoreFactory.Create(tenant);

        var assignments = await userRoleStore
            .GetAssignmentsAsync(context.ActorUserKey.Value, ct);

        return assignments.Any(x =>
            x.RoleId == LabRoleIds.Admin);
    }

    public async Task EnsureAdminAsync(AccessContext context, CancellationToken ct = default)
    {
        if (!await IsAdminAsync(context, ct))
        {
            throw new UnauthorizedAccessException("LabAdmin permission is required.");
        }
    }
}
