using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionStore<TUserId>
    {
        /// <summary>Retrieve a session instance by session id.</summary>
        Task<ISession<TUserId>?> GetSessionAsync(AuthSessionId sessionId);

        /// <summary>Persist or update a session instance.</summary>
        Task SaveSessionAsync(ISession<TUserId> session);

        /// <summary>Mark a session as revoked.</summary>
        Task RevokeSessionAsync(AuthSessionId sessionId, DateTime at);

        /// <summary>List all sessions that belong to a chain.</summary>
        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsByChainAsync(ChainId chainId);


        /// <summary>Retrieve a session chain by chain id.</summary>
        Task<ISessionChain<TUserId>?> GetChainAsync(ChainId chainId);

        /// <summary>Insert a new chain.</summary>
        Task SaveChainAsync(ISessionChain<TUserId> chain);

        /// <summary>Update an existing chain (rotation count, revoked state, etc).</summary>
        Task UpdateChainAsync(ISessionChain<TUserId> chain);

        /// <summary>Mark a chain as revoked (device-level logout).</summary>
        Task RevokeChainAsync(ChainId chainId, DateTime at);

        /// <summary>
        /// Get the active session id for this chain.
        /// Should be O(1) lookup (Redis Hash / indexed column / simple get).
        /// </summary>
        Task<AuthSessionId?> GetActiveSessionIdAsync(ChainId chainId);

        /// <summary>
        /// Set the active session id for this chain.
        /// MUST be atomic, replaces the previous active session.
        /// </summary>
        Task SetActiveSessionIdAsync(ChainId chainId, AuthSessionId sessionId);

        /// <summary>List all chains for a given user.</summary>
        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsByUserAsync(TUserId userId);


        /// <summary>Retrieve the user's session root structure.</summary>
        Task<ISessionRoot<TUserId>?> GetSessionRootAsync(TUserId userId);

        /// <summary>Save or create a new user session root.</summary>
        Task SaveSessionRootAsync(ISessionRoot<TUserId> root);

        /// <summary>Mark the user's session root as revoked.</summary>
        Task RevokeSessionRootAsync(TUserId userId, DateTime at);


        /// <summary>
        /// Remove expired sessions from the store.
        /// Chains and roots may remain, but sessions inside them are removed.
        /// Store implementation chooses optimal cleanup strategy.
        /// </summary>
        Task DeleteExpiredSessionsAsync(DateTime now);
    }

}
