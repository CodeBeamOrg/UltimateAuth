using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Runtime, read-only user access for authentication flows.
/// Not a domain store.
/// </summary>
public interface IUserRuntimeStateProvider
{
    Task<UserRuntimeRecord?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
}
