using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core;

public interface IUserClaimsProvider
{
    Task<ClaimsSnapshot> GetClaimsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
}
