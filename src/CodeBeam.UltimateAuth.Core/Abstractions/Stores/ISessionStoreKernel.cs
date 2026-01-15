using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionStoreKernel
    {
        Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
        //string? TenantId { get; }

        // Session
        Task<ISession?> GetSessionAsync(AuthSessionId sessionId);
        Task SaveSessionAsync(ISession session);
        Task RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at);

        // Chain
        Task<ISessionChain?> GetChainAsync(SessionChainId chainId);
        Task SaveChainAsync(ISessionChain chain);
        Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at);
        Task<AuthSessionId?> GetActiveSessionIdAsync(SessionChainId chainId);
        Task SetActiveSessionIdAsync(SessionChainId chainId, AuthSessionId sessionId);

        // Root
        Task<ISessionRoot?> GetSessionRootByUserAsync(UserKey userKey);
        Task<ISessionRoot?> GetSessionRootByIdAsync(SessionRootId rootId);
        Task SaveSessionRootAsync(ISessionRoot root);
        Task RevokeSessionRootAsync(UserKey userKey, DateTimeOffset at);

        // Helpers
        Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId);
        Task<IReadOnlyList<ISessionChain>> GetChainsByUserAsync(UserKey userKey);
        Task<IReadOnlyList<ISession>> GetSessionsByChainAsync(SessionChainId chainId);

        // Maintenance
        Task DeleteExpiredSessionsAsync(DateTimeOffset at);
    }
}
