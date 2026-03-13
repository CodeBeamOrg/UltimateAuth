using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class RolePermissionResolver : IRolePermissionResolver
{
    private readonly IRoleStore _roles;

    public RolePermissionResolver(IRoleStore roles)
    {
        _roles = roles;
    }

    public async Task<IReadOnlyCollection<Permission>> ResolveAsync(TenantKey tenant, IReadOnlyCollection<RoleId> roleIds, CancellationToken ct = default)
    {
        if (roleIds.Count == 0)
            return Array.Empty<Permission>();

        var roles = await _roles.GetByIdsAsync(tenant, roleIds, ct);

        var permissions = new HashSet<Permission>();

        foreach (var role in roles)
        {
            foreach (var perm in role.Permissions)
                permissions.Add(perm);
        }

        return permissions.ToArray();
    }
}
