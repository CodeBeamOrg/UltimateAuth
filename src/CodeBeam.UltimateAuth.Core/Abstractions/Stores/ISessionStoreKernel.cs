using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ISessionStoreKernel
{
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);

    Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId);
    Task SaveSessionAsync(UAuthSession session);
    Task<bool> RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at);

    Task<UAuthSessionChain?> GetChainAsync(SessionChainId chainId);
    Task SaveChainAsync(UAuthSessionChain chain);
    Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at);
    Task<AuthSessionId?> GetActiveSessionIdAsync(SessionChainId chainId);
    Task SetActiveSessionIdAsync(SessionChainId chainId, AuthSessionId sessionId);

    Task<UAuthSessionRoot?> GetSessionRootByUserAsync(UserKey userKey);
    Task<UAuthSessionRoot?> GetSessionRootByIdAsync(SessionRootId rootId);
    Task SaveSessionRootAsync(UAuthSessionRoot root);
    Task RevokeSessionRootAsync(UserKey userKey, DateTimeOffset at);

    Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId);
    Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey);
    Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId);
    Task DeleteExpiredSessionsAsync(DateTimeOffset at);
}
