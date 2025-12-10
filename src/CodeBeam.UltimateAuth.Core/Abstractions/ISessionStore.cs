using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionStore<TUserId>
    {
        Task<ISession<TUserId>?> GetSessionAsync(AuthSessionId sessionId);

        Task SaveSessionAsync(ISession<TUserId> session);

        Task RevokeSessionAsync(AuthSessionId sessionId, DateTime at);

        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsByChainAsync(ChainId chainId);

        Task<ISessionChain<TUserId>?> GetChainAsync(ChainId chainId);

        Task SaveChainAsync(ISessionChain<TUserId> chain);

        Task UpdateChainAsync(ISessionChain<TUserId> chain);

        Task RevokeChainAsync(ChainId chainId, DateTime at);

        Task<AuthSessionId?> GetActiveSessionIdAsync(ChainId chainId);

        Task SetActiveSessionIdAsync(ChainId chainId, AuthSessionId sessionId);

        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsByUserAsync(TUserId userId);

        Task<ISessionRoot<TUserId>?> GetSessionRootAsync(TUserId userId);


        Task SaveSessionRootAsync(ISessionRoot<TUserId> root);

        Task RevokeSessionRootAsync(TUserId userId, DateTime at);


        /// <summary>
        /// Remove expired sessions from the store.
        /// Chains and roots may remain, but sessions inside them are removed.
        /// Store implementation chooses optimal cleanup strategy.
        /// </summary>
        Task DeleteExpiredSessionsAsync(DateTime now);
    }

}
