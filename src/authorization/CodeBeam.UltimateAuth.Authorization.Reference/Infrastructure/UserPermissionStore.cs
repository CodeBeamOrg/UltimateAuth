using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class UserPermissionStore : IUserPermissionStore
{
    private readonly IUserRoleStore _userRoles;
    private readonly IRolePermissionResolver _resolver;

    public UserPermissionStore(IUserRoleStore userRoles, IRolePermissionResolver resolver)
    {
        _userRoles = userRoles;
        _resolver = resolver;
    }

    public async Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var assignments = await _userRoles.GetAssignmentsAsync(tenant, userKey, ct);
        var roleIds = assignments.Select(x => x.RoleId).ToArray();
        return await _resolver.ResolveAsync(tenant, roleIds, ct);
    }
}
