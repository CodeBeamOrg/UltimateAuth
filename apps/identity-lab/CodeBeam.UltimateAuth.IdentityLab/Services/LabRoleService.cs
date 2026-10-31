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

        var expectedRoleId = roleName switch
        {
            LabRoles.Owner => LabRoleIds.Owner,
            LabRoles.User => LabRoleIds.User,
            _ => throw new InvalidOperationException("Unsupported role assignment.")
        };

        var roleStore = roleStoreFactory.Create(tenant);
        var userRoleStore = userRoleStoreFactory.Create(tenant);

        var role = await roleStore.GetByNameAsync(roleName.ToUpperInvariant(), ct);

        if (role is null || role.Id != expectedRoleId)
        {
            throw new InvalidOperationException($"System role '{roleName}' is missing or has an invalid identity.");
        }

        var assignments = await userRoleStore.GetAssignmentsAsync(userKey, ct);

        if (assignments.Any(x => x.RoleId == expectedRoleId))
            return;

        await userRoleStore.AssignAsync(userKey, expectedRoleId, DateTimeOffset.UtcNow, ct);
    }
}
