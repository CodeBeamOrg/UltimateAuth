using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users;

public interface IUserSecurityStateProvider
{
    Task<IUserSecurityState?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
}
