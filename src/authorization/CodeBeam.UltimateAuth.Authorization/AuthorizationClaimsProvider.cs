using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Constants;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Authorization;

public sealed class AuthorizationClaimsProvider : IUserClaimsProvider
{
    private readonly IUserRoleStore _roles;
    private readonly IRoleStore _roleStore;
    private readonly IUserPermissionStore _permissions;

    public AuthorizationClaimsProvider(IUserRoleStore roles, IRoleStore roleStore, IUserPermissionStore permissions)
    {
        _roles = roles;
        _roleStore = roleStore;
        _permissions = permissions;
    }

    public async Task<ClaimsSnapshot> GetClaimsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var roleIds = await _roles.GetRolesAsync(tenant, userKey, ct);
        var roles = await _roleStore.GetByIdsAsync(tenant, roleIds, ct);
        var perms = await _permissions.GetPermissionsAsync(tenant, userKey, ct);

        var builder = ClaimsSnapshot.Create();

        builder.Add(UAuthConstants.Claims.Tenant, tenant.Value);

        foreach (var role in roles)
            builder.Add(ClaimTypes.Role, role.Name);

        foreach (var perm in perms)
            builder.Add(UAuthConstants.Claims.Permission, perm.Value);

        return builder.Build();
    }
}
