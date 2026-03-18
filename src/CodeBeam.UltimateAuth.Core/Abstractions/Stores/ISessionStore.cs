using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ISessionStore
{
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);

    Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId, CancellationToken ct = default);
    Task SaveSessionAsync(UAuthSession session, long expectedVersion, CancellationToken ct = default);
    Task CreateSessionAsync(UAuthSession session, CancellationToken ct = default);
    Task<bool> RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default);
    public Task RevokeOtherSessionsAsync(UserKey user, SessionChainId keepChain, DateTimeOffset at, CancellationToken ct = default);
    public Task RevokeAllSessionsAsync(UserKey user, DateTimeOffset at, CancellationToken ct = default);
    Task RemoveSessionAsync(AuthSessionId sessionId, CancellationToken ct = default);

    Task<UAuthSessionChain?> GetChainAsync(SessionChainId chainId, CancellationToken ct = default);
    Task SaveChainAsync(UAuthSessionChain chain, long expectedVersion, CancellationToken ct = default);
    Task CreateChainAsync(UAuthSessionChain chain, CancellationToken ct = default);
    Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default);
    Task RevokeOtherChainsAsync(UserKey user, SessionChainId keepChain, DateTimeOffset at, CancellationToken ct = default);
    Task RevokeAllChainsAsync(UserKey user, DateTimeOffset at, CancellationToken ct = default);
    Task RevokeChainCascadeAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default);
    Task LogoutChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default);

    Task<UAuthSessionRoot?> GetRootByUserAsync(UserKey userKey, CancellationToken ct = default);
    Task<UAuthSessionRoot?> GetRootByIdAsync(SessionRootId rootId, CancellationToken ct = default);
    Task SaveRootAsync(UAuthSessionRoot root, long expectedVersion, CancellationToken ct = default);
    Task CreateRootAsync(UAuthSessionRoot root, CancellationToken ct = default);
    Task RevokeRootAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default);
    Task RevokeRootCascadeAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default);

    Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey, bool includeHistoricalRoots = false, CancellationToken ct = default);
    Task<UAuthSessionChain?> GetChainByDeviceAsync(UserKey userKey, DeviceId deviceId, CancellationToken ct = default);
    Task<IReadOnlyList<UAuthSessionChain>> GetChainsByRootAsync(SessionRootId rootId, CancellationToken ct = default);
    Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId, CancellationToken ct = default);
}
