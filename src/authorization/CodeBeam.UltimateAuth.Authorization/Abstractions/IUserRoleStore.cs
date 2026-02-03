using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserRoleStore
{
    Task AssignAsync(TenantKey tenant, UserKey userKey, string role, CancellationToken ct = default);
    Task RemoveAsync(TenantKey tenant, UserKey userKey, string role, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetRolesAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
}
