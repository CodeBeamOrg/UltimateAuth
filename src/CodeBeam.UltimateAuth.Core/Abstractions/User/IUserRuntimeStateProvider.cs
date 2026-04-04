using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Runtime, read-only user access for authentication flows.
/// Not a domain store.
/// </summary>
public interface IUserRuntimeStateProvider
{
    Task<UserRuntimeRecord?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
}
