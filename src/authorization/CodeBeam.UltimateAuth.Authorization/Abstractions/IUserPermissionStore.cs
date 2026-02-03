using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserPermissionStore
{
    Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
}
