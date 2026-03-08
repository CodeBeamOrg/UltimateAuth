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
        var roles = await _userRoles.GetRolesAsync(tenant, userKey, ct);
        return await _resolver.ResolveAsync(tenant, roles, ct);
    }
}
