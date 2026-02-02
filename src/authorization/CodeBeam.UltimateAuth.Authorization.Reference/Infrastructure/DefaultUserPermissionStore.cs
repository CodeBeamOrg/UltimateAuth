using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.Reference
{
    public sealed class DefaultUserPermissionStore : IUserPermissionStore
    {
        private readonly IUserRoleStore _roles;
        private readonly IRolePermissionResolver _resolver;

        public DefaultUserPermissionStore(IUserRoleStore roles, IRolePermissionResolver resolver)
        {
            _roles = roles;
            _resolver = resolver;
        }

        public async Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
        {
            var roles = await _roles.GetRolesAsync(tenant, userKey, ct);
            return await _resolver.ResolveAsync(tenant, roles, ct);
        }
    }

}
