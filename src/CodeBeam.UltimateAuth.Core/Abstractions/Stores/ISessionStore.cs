using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionStore<TUserId>
    {
        Task<ISession<TUserId>?> GetSessionAsync(string? tenantId, AuthSessionId sessionId);

        Task SaveSessionAsync(string? tenantId, ISession<TUserId> session);

        Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime at);

        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsByChainAsync(string? tenantId, ChainId chainId);

        Task<ISessionChain<TUserId>?> GetChainAsync(string? tenantId, ChainId chainId);

        Task SaveChainAsync(string? tenantId, ISessionChain<TUserId> chain);

        Task UpdateChainAsync(string? tenantId, ISessionChain<TUserId> chain);

        Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTime at);

        Task<AuthSessionId?> GetActiveSessionIdAsync(string? tenantId, ChainId chainId);

        Task SetActiveSessionIdAsync(string? tenantId, ChainId chainId, AuthSessionId sessionId);

        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsByUserAsync(string? tenantId, TUserId userId);

        Task<ISessionRoot<TUserId>?> GetSessionRootAsync(string? tenantId, TUserId userId);


        Task SaveSessionRootAsync(string? tenantId, ISessionRoot<TUserId> root);

        Task RevokeSessionRootAsync(string? tenantId, TUserId userId, DateTime at);


        /// <summary>
        /// Remove expired sessions from the store.
        /// Chains and roots may remain, but sessions inside them are removed.
        /// Store implementation chooses optimal cleanup strategy.
        /// </summary>
        Task DeleteExpiredSessionsAsync(string? tenantId, DateTime now);
        Task<ChainId?> GetChainIdBySessionAsync(string? tenantId, AuthSessionId sessionId);
    }

}
