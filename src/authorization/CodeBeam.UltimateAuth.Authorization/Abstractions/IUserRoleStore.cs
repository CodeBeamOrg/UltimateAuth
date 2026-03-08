using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserRoleStore
{
    Task<IReadOnlyCollection<RoleId>> GetRolesAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
    Task AssignAsync(TenantKey tenant, UserKey userKey, RoleId roleId, CancellationToken ct = default);
    Task RemoveAsync(TenantKey tenant, UserKey userKey, RoleId roleId, CancellationToken ct = default);
}
