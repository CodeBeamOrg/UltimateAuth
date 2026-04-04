using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class UserPermissionStore : IUserPermissionStore
{
    private readonly IUserRoleStoreFactory _userRolesFactory;
    private readonly IRolePermissionResolver _resolver;

    public UserPermissionStore(IUserRoleStoreFactory userRolesFactory, IRolePermissionResolver resolver)
    {
        _userRolesFactory = userRolesFactory;
        _resolver = resolver;
    }

    public async Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var userRoleStore = _userRolesFactory.Create(tenant);
        var assignments = await userRoleStore.GetAssignmentsAsync(userKey, ct);
        var roleIds = assignments.Select(x => x.RoleId).ToArray();
        return await _resolver.ResolveAsync(tenant, roleIds, ct);
    }
}
