using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.InMemory;

internal sealed class InMemorySessionStoreKernel<TUserId> : ISessionStoreKernel<TUserId>
{
    private readonly SemaphoreSlim _tx = new(1, 1);

    private readonly ConcurrentDictionary<AuthSessionId, ISession<TUserId>> _sessions = new();
    private readonly ConcurrentDictionary<ChainId, ISessionChain<TUserId>> _chains = new();
    private readonly ConcurrentDictionary<TUserId, ISessionRoot<TUserId>> _roots = new();
    private readonly ConcurrentDictionary<ChainId, AuthSessionId> _activeSessions = new();

    public async Task ExecuteAsync(Func<Task> action)
    {
        await _tx.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            _tx.Release();
        }
    }

    public Task<ISession<TUserId>?> GetSessionAsync(string? _, AuthSessionId sessionId)
        => Task.FromResult(
            _sessions.TryGetValue(sessionId, out var s) ? s : null);

    public Task SaveSessionAsync(string? _, ISession<TUserId> session)
    {
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task RevokeSessionAsync(string? _, AuthSessionId sessionId, DateTimeOffset at)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions[sessionId] = session.Revoke(at);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ISession<TUserId>>> GetSessionsByChainAsync(string? _, ChainId chainId)
    {
        var result = _sessions.Values
            .Where(s => s.ChainId == chainId)
            .ToList();

        return Task.FromResult<IReadOnlyList<ISession<TUserId>>>(result);
    }

    public Task<ISessionChain<TUserId>?> GetChainAsync(string? _, ChainId chainId)
        => Task.FromResult(
            _chains.TryGetValue(chainId, out var c) ? c : null);

    public Task SaveChainAsync(string? _, ISessionChain<TUserId> chain)
    {
        _chains[chain.ChainId] = chain;
        return Task.CompletedTask;
    }

    public Task RevokeChainAsync(string? _, ChainId chainId, DateTimeOffset at)
    {
        if (_chains.TryGetValue(chainId, out var chain))
        {
            _chains[chainId] = chain.Revoke(at);
        }
        return Task.CompletedTask;
    }

    public Task<AuthSessionId?> GetActiveSessionIdAsync(string? _, ChainId chainId)
    {
        return Task.FromResult<AuthSessionId?>(
            _activeSessions.TryGetValue(chainId, out var id)
                ? id
                : null
        );
    }

    public Task SetActiveSessionIdAsync(string? _, ChainId chainId, AuthSessionId sessionId)
    {
        _activeSessions[chainId] = sessionId;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsByUserAsync(string? _, TUserId userId)
    {
        if (!_roots.TryGetValue(userId, out var root))
            return Task.FromResult<IReadOnlyList<ISessionChain<TUserId>>>(Array.Empty<ISessionChain<TUserId>>());

        return Task.FromResult<IReadOnlyList<ISessionChain<TUserId>>>(root.Chains.ToList());
    }

    public Task<ISessionRoot<TUserId>?> GetSessionRootAsync(string? _, TUserId userId)
        => Task.FromResult(_roots.TryGetValue(userId, out var r) ? r : null);

    public Task SaveSessionRootAsync(string? _, ISessionRoot<TUserId> root)
    {
        _roots[root.UserId] = root;
        return Task.CompletedTask;
    }

    public Task RevokeSessionRootAsync(string? _, TUserId userId, DateTimeOffset at)
    {
        if (_roots.TryGetValue(userId, out var root))
        {
            _roots[userId] = root.Revoke(at);
        }
        return Task.CompletedTask;
    }

    public Task DeleteExpiredSessionsAsync(string? _, DateTimeOffset now)
    {
        foreach (var kvp in _sessions)
        {
            var session = kvp.Value;

            if (session.ExpiresAt <= now)
            {
                _sessions.TryGetValue(kvp.Key, out var existing);

                if (existing is not null)
                {
                    _sessions.TryUpdate(
                        kvp.Key,
                        existing.Revoke(now),
                        existing);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<ChainId?> GetChainIdBySessionAsync(string? _, AuthSessionId sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            return Task.FromResult<ChainId?>(session.ChainId);

        return Task.FromResult<ChainId?>(null);
    }
}
