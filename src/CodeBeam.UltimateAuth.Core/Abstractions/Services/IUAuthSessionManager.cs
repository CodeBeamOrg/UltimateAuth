using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Provides high-level session lifecycle operations such as creation, refresh, validation, and revocation.
/// </summary>
public interface IUAuthSessionManager
{
    Task<IReadOnlyList<ISessionChain>> GetChainsAsync(string? tenantId, UserKey userKey);

    Task<IReadOnlyList<ISession>> GetSessionsAsync(string? tenantId, SessionChainId chainId);

    Task<ISession?> GetCurrentSessionAsync(string? tenantId, AuthSessionId sessionId);

    Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at);

    Task RevokeChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset at);

    Task<SessionChainId?> ResolveChainIdAsync(string? tenantId, AuthSessionId sessionId);

    Task RevokeAllChainsAsync(string? tenantId, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at);
    
    // Hard revoke - admin
    Task RevokeRootAsync(string? tenantId, UserKey userKey, DateTimeOffset at);
}
