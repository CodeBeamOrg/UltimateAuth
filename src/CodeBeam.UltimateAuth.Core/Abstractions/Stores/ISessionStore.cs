using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// High-level session store abstraction used by UltimateAuth.
    /// Encapsulates session, chain, and root orchestration.
    /// </summary>
    public interface ISessionStore<TUserId>
    {
        /// <summary>
        /// Retrieves an active session by id.
        /// </summary>
        Task<ISession<TUserId>?> GetSessionAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default);

        /// <summary>
        /// Creates a new session and associates it with the appropriate chain and root.
        /// </summary>
        Task CreateSessionAsync(IssuedSession<TUserId> issuedSession, SessionStoreContext<TUserId> context, CancellationToken ct = default);

        /// <summary>
        /// Refreshes (rotates) the active session within its chain.
        /// </summary>
        Task RotateSessionAsync(AuthSessionId currentSessionId, IssuedSession<TUserId> newSession, SessionStoreContext<TUserId> context, CancellationToken ct = default);

        /// <summary>
        /// Revokes a single session.
        /// </summary>
        Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default);

        /// <summary>
        /// Revokes all sessions for a specific user (all devices).
        /// </summary>
        Task RevokeAllSessionsAsync(string? tenantId, TUserId userId, DateTimeOffset at, CancellationToken ct = default);

        /// <summary>
        /// Revokes all sessions within a specific chain (single device).
        /// </summary>
        Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTimeOffset at, CancellationToken ct = default);
    }
}
