using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// High-level session store abstraction used by UltimateAuth.
/// Encapsulates session, chain, and root orchestration.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Retrieves an active session by id.
    /// </summary>
    Task<ISession?> GetSessionAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new session and associates it with the appropriate chain and root.
    /// </summary>
    Task CreateSessionAsync(IssuedSession issuedSession, SessionStoreContext context, CancellationToken ct = default);

    /// <summary>
    /// Refreshes (rotates) the active session within its chain.
    /// </summary>
    Task RotateSessionAsync(AuthSessionId currentSessionId, IssuedSession newSession, SessionStoreContext context, CancellationToken ct = default);

    Task<bool> TouchSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, SessionTouchMode mode = SessionTouchMode.IfNeeded, CancellationToken ct = default);

    /// <summary>
    /// Revokes a single session.
    /// </summary>
    Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default);

    /// <summary>
    /// Revokes all sessions for a specific user (all devices).
    /// </summary>
    Task RevokeAllChainsAsync(string? tenantId, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default);

    /// <summary>
    /// Revokes all sessions within a specific chain (single device).
    /// </summary>
    Task RevokeChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default);

    Task RevokeRootAsync(string? tenantId, UserKey userKey, DateTimeOffset at, CancellationToken ct = default);
}
