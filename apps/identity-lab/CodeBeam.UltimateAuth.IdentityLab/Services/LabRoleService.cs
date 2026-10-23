using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabRoleService(IRoleStoreFactory roleStoreFactory, IUserRoleStoreFactory userRoleStoreFactory)
{
    public async Task AssignAsync(TenantKey tenant, UserKey userKey, string roleName, CancellationToken ct = default)
    {
        if (roleName != LabRoles.Owner && roleName != LabRoles.User)
        {
            throw new InvalidOperationException("Unsupported role assignment.");
        }

        var roleStore = roleStoreFactory.Create(tenant);
        var userRoleStore = userRoleStoreFactory.Create(tenant);

        var role = await roleStore.GetByNameAsync(roleName.ToUpperInvariant(), ct);

        if (role is null)
            throw new InvalidOperationException($"System role '{roleName}' was not found.");

        var assignments = await userRoleStore.GetAssignmentsAsync(userKey, ct);

        if (assignments.Any(x => x.RoleId == role.Id))
            return;

        await userRoleStore.AssignAsync(userKey, role.Id, DateTimeOffset.UtcNow, ct);
    }
}
