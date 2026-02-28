using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ISessionStore
{
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);

    Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId);
    Task SaveSessionAsync(UAuthSession session, long expectedVersion);
    Task CreateSessionAsync(UAuthSession session);
    Task<bool> RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at);

    Task<UAuthSessionChain?> GetChainAsync(SessionChainId chainId);
    Task SaveChainAsync(UAuthSessionChain chain, long expectedVersion);
    Task CreateChainAsync(UAuthSessionChain chain);
    Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at);
    Task RevokeChainCascadeAsync(SessionChainId chainId, DateTimeOffset at);

    Task<UAuthSessionRoot?> GetRootByUserAsync(UserKey userKey);
    Task<UAuthSessionRoot?> GetRootByIdAsync(SessionRootId rootId);
    Task SaveRootAsync(UAuthSessionRoot root, long expectedVersion);
    Task CreateRootAsync(UAuthSessionRoot root);
    Task RevokeRootAsync(UserKey userKey, DateTimeOffset at);
    Task RevokeRootCascadeAsync(UserKey userKey, DateTimeOffset at);

    Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId);
    Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey);
    Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId);
    Task DeleteExpiredSessionsAsync(DateTimeOffset at);
}
