namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionStore<TUserId>
    {
        // ---- SESSION (LEVEL 3) ----
        Task<IAuthSession<TUserId>?> GetSessionAsync(AuthSessionId sessionId);
        Task SaveSessionAsync(IAuthSession<TUserId> session);
        Task RevokeSessionAsync(AuthSessionId sessionId, DateTime at);

        // ---- CHAIN (LEVEL 2) ----
        Task<IAuthSessionChain<TUserId>?> GetChainAsync(ChainId chainId);
        Task SaveChainAsync(IAuthSessionChain<TUserId> chain);
        Task UpdateChainAsync(IAuthSessionChain<TUserId> chain);
        Task RevokeChainAsync(ChainId chainId, DateTime at);

        // Active session (very important)
        Task<AuthSessionId?> GetActiveSessionIdAsync(ChainId chainId);
        Task SetActiveSessionIdAsync(ChainId chainId, AuthSessionId sessionId);

        // ---- USER ROOT (LEVEL 1) ----
        Task<UserSessionRoot<TUserId>?> GetUserRootAsync(TUserId userId);
        Task SaveUserRootAsync(UserSessionRoot<TUserId> root);
        Task RevokeUserRootAsync(TUserId userId, DateTime at);

        // ---- LOOKUPS ----
        Task<IReadOnlyList<IAuthSessionChain<TUserId>>> GetChainsByUserAsync(TUserId userId);
        Task<IReadOnlyList<IAuthSession<TUserId>>> GetSessionsByChainAsync(ChainId chainId);

        // ---- CLEANUP ----
        Task DeleteExpiredSessionsAsync(DateTime now);
    }


}
