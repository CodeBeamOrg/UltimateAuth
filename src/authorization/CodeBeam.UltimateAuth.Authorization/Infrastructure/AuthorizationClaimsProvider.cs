using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Authorization;

public sealed class AuthorizationClaimsProvider : IUserClaimsProvider
{
    private readonly IUserRoleStoreFactory _userRoleFactory;
    private readonly IRoleStoreFactory _roleFactory;
    private readonly IUserPermissionStore _permissions;

    public AuthorizationClaimsProvider(IUserRoleStoreFactory userRoleFactory, IRoleStoreFactory roleFactory, IUserPermissionStore permissions)
    {
        _userRoleFactory = userRoleFactory;
        _roleFactory = roleFactory;
        _permissions = permissions;
    }

    public async Task<ClaimsSnapshot> GetClaimsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var roleStore = _roleFactory.Create(tenant);
        var userRoleStore = _userRoleFactory.Create(tenant);
        var assignments = await userRoleStore.GetAssignmentsAsync(userKey, ct);
        var roleIds = assignments.Select(x => x.RoleId).Distinct().ToArray();
        var roles = await roleStore.GetByIdsAsync(roleIds, ct);
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
