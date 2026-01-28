using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Abstractions;

/// <summary>
/// Runtime, read-only user access for authentication flows.
/// Not a domain store.
/// </summary>
public interface IUserRuntimeStore
{
    Task<UserRuntimeRecord?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
}
