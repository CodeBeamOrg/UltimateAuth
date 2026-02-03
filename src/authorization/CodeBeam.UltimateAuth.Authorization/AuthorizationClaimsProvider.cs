using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Authorization;

public sealed class AuthorizationClaimsProvider : IUserClaimsProvider
{
    private readonly IUserRoleStore _roles;
    private readonly IUserPermissionStore _permissions;

    public AuthorizationClaimsProvider(IUserRoleStore roles, IUserPermissionStore permissions)
    {
        _roles = roles;
        _permissions = permissions;
    }

    public async Task<ClaimsSnapshot> GetClaimsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var roles = await _roles.GetRolesAsync(tenant, userKey, ct);
        var perms = await _permissions.GetPermissionsAsync(tenant, userKey, ct);

        var builder = ClaimsSnapshot.Create();

        foreach (var role in roles)
            builder.Add(ClaimTypes.Role, role);

        foreach (var perm in perms)
            builder.Add("uauth:permission", perm.Value);

        return builder.Build();
    }
}
