using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Defines the low-level persistence operations for sessions, session chains, and session roots in a multi-tenant or single-tenant environment.
    /// Store implementations provide durable and atomic data access.
    /// </summary>
    public interface ISessionStore<TUserId>
    {
        /// <summary>
        /// Retrieves a session by its identifier within the given tenant context.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c> for single-tenant mode.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>The session instance or <c>null</c> if not found.</returns>
        Task<ISession<TUserId>?> GetSessionAsync(string? tenantId, AuthSessionId sessionId);

        /// <summary>
        /// Persists a new session or updates an existing one within the tenant scope.
        /// Implementations must ensure atomic writes.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="session">The session to persist.</param>
        Task SaveSessionAsync(string? tenantId, ISession<TUserId> session);

        /// <summary>
        /// Marks the specified session as revoked, preventing future authentication.
        /// Revocation timestamp must be stored reliably.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="at">The UTC timestamp of revocation.</param>
        Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime at);
        
        /// <summary>
        /// Returns all sessions belonging to the specified chain, ordered according to store implementation rules.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="chainId">The chain identifier.</param>
        /// <returns>A read-only list of sessions.</returns>
        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsByChainAsync(string? tenantId, ChainId chainId);

        /// <summary>
        /// Retrieves a session chain by identifier. Returns <c>null</c> if the chain does not exist in the provided tenant context.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="chainId">The chain identifier.</param>
        /// <returns>The chain or <c>null</c>.</returns>
        Task<ISessionChain<TUserId>?> GetChainAsync(string? tenantId, ChainId chainId);

        /// <summary>
        /// Inserts a new session chain into the store. Implementations must ensure consistency with the related sessions and session root.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="chain">The chain to save.</param>
        Task SaveChainAsync(string? tenantId, ISessionChain<TUserId> chain);

        /// <summary>
        /// Updates an existing session chain, typically after session rotation or revocation. Implementations must preserve atomicity.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="chain">The updated session chain.</param>
        Task UpdateChainAsync(string? tenantId, ISessionChain<TUserId> chain);

        /// <summary>
        /// Marks the entire session chain as revoked, invalidating all associated sessions for the device or app family.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="chainId">The chain to revoke.</param>
        /// <param name="at">The UTC timestamp of revocation.</param>
        Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTime at);

        /// <summary>
        /// Retrieves the active session identifier for the specified chain.  
        /// This is typically an O(1) lookup and used for session rotation.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="chainId">The chain whose active session is requested.</param>
        /// <returns>The active session identifier or <c>null</c>.</returns>
        Task<AuthSessionId?> GetActiveSessionIdAsync(string? tenantId, ChainId chainId);

        /// <summary>
        /// Sets or replaces the active session identifier for the specified chain.
        /// Must be atomic to prevent race conditions during refresh.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="chainId">The chain whose active session is being set.</param>
        /// <param name="sessionId">The new active session identifier.</param>
        Task SetActiveSessionIdAsync(string? tenantId, ChainId chainId, AuthSessionId sessionId);

        /// <summary>
        /// Retrieves all session chains belonging to the specified user within the tenant scope.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="userId">The user whose chains are being retrieved.</param>
        /// <returns>A read-only list of session chains.</returns>
        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsByUserAsync(string? tenantId, TUserId userId);

        /// <summary>
        /// Retrieves the session root for the user, which represents the full set of chains and their associated security metadata.
        /// Returns <c>null</c> if the root does not exist.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The session root or <c>null</c>.</returns>
        Task<ISessionRoot<TUserId>?> GetSessionRootAsync(string? tenantId, TUserId userId);

        /// <summary>
        /// Persists a session root structure, usually after chain creation, rotation, or security operations.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="root">The session root to save.</param>
        Task SaveSessionRootAsync(string? tenantId, ISessionRoot<TUserId> root);

        /// <summary>
        /// Revokes the session root, invalidating all chains and sessions belonging to the specified user in the tenant scope.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="userId">The user whose root should be revoked.</param>
        /// <param name="at">The UTC timestamp of revocation.</param>
        Task RevokeSessionRootAsync(string? tenantId, TUserId userId, DateTime at);

        /// <summary>
        /// Removes expired sessions from the store while leaving chains and session roots intact. Cleanup strategy is determined by the store implementation.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="now">The current UTC timestamp.</param>
        Task DeleteExpiredSessionsAsync(string? tenantId, DateTime now);

        /// <summary>
        /// Retrieves the chain identifier associated with the specified session.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>The chain identifier or <c>null</c>.</returns>
        Task<ChainId?> GetChainIdBySessionAsync(string? tenantId, AuthSessionId sessionId);
    }

}
