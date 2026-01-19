using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Authorization
{
    public sealed class DefaultAuthorizationClaimsProvider : IUserClaimsProvider
    {
        private readonly IUserRoleStore _roles;
        private readonly IUserPermissionStore _permissions;

        public DefaultAuthorizationClaimsProvider(IUserRoleStore roles, IUserPermissionStore permissions)
        {
            _roles = roles;
            _permissions = permissions;
        }

        public async Task<ClaimsSnapshot> GetClaimsAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        {
            var roles = await _roles.GetRolesAsync(tenantId, userKey, ct);
            var perms = await _permissions.GetPermissionsAsync(tenantId, userKey, ct);

            var builder = ClaimsSnapshot.Create();

            foreach (var role in roles)
                builder.Add(ClaimTypes.Role, role);

            foreach (var perm in perms)
                builder.Add("uauth:permission", perm.Value);

            return builder.Build();
        }
    }

}
