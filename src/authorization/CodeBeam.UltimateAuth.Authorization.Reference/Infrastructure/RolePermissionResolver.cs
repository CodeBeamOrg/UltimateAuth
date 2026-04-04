using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class RolePermissionResolver : IRolePermissionResolver
{
    private readonly IRoleStoreFactory _roleFactory;

    public RolePermissionResolver(IRoleStoreFactory roleFactory)
    {
        _roleFactory = roleFactory;
    }

    public async Task<IReadOnlyCollection<Permission>> ResolveAsync(TenantKey tenant, IReadOnlyCollection<RoleId> roleIds, CancellationToken ct = default)
    {
        if (roleIds.Count == 0)
            return Array.Empty<Permission>();

        var roleStore = _roleFactory.Create(tenant);
        var roles = await roleStore.GetByIdsAsync(roleIds, ct);

        var permissions = new HashSet<Permission>();

        foreach (var role in roles)
        {
            foreach (var perm in role.Permissions)
                permissions.Add(perm);
        }

        return permissions.ToArray();
    }
}
