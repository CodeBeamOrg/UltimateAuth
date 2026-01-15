using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using System.Collections.Concurrent;

internal sealed class InMemorySessionStoreKernel : ISessionStoreKernel, ITenantAwareSessionStore
{
    private readonly SemaphoreSlim _tx = new(1, 1);

    private readonly ConcurrentDictionary<AuthSessionId, ISession> _sessions = new();
    private readonly ConcurrentDictionary<SessionChainId, ISessionChain> _chains = new();
    private readonly ConcurrentDictionary<UserKey, ISessionRoot> _roots = new();
    private readonly ConcurrentDictionary<SessionChainId, AuthSessionId> _activeSessions = new();

    public string? TenantId { get; private set; }

    public void BindTenant(string? tenantId)
    {
        TenantId = tenantId ?? "__single__";
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        await _tx.WaitAsync(ct);
        try
        {
            await action(ct);
        }
        finally
        {
            _tx.Release();
        }
    }

    public Task<ISession?> GetSessionAsync(AuthSessionId sessionId)
        => Task.FromResult(_sessions.TryGetValue(sessionId, out var s) ? s : null);

    public Task SaveSessionAsync(ISession session)
    {
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions[sessionId] = session.Revoke(at);
        }
        return Task.CompletedTask;
    }

    public Task<ISessionChain?> GetChainAsync(SessionChainId chainId)
        => Task.FromResult(_chains.TryGetValue(chainId, out var c) ? c : null);

    public Task SaveChainAsync(ISessionChain chain)
    {
        _chains[chain.ChainId] = chain;
        return Task.CompletedTask;
    }

    public Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at)
    {
        if (_chains.TryGetValue(chainId, out var chain))
        {
            _chains[chainId] = chain.Revoke(at);
        }
        return Task.CompletedTask;
    }

    public Task<AuthSessionId?> GetActiveSessionIdAsync(SessionChainId chainId)
        => Task.FromResult<AuthSessionId?>(_activeSessions.TryGetValue(chainId, out var id) ? id : null);

    public Task SetActiveSessionIdAsync(SessionChainId chainId, AuthSessionId sessionId)
    {
        _activeSessions[chainId] = sessionId;
        return Task.CompletedTask;
    }

    public Task<ISessionRoot?> GetSessionRootByUserAsync(UserKey userKey)
        => Task.FromResult(_roots.TryGetValue(userKey, out var r) ? r : null);

    public Task<ISessionRoot?> GetSessionRootByIdAsync(SessionRootId rootId)
        => Task.FromResult(_roots.Values.FirstOrDefault(r => r.RootId == rootId));

    public Task SaveSessionRootAsync(ISessionRoot root)
    {
        _roots[root.UserKey] = root;
        return Task.CompletedTask;
    }

    public Task RevokeSessionRootAsync(UserKey userKey, DateTimeOffset at)
    {
        if (_roots.TryGetValue(userKey, out var root))
        {
            _roots[userKey] = root.Revoke(at);
        }
        return Task.CompletedTask;
    }

    public Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            return Task.FromResult<SessionChainId?>(session.ChainId);

        return Task.FromResult<SessionChainId?>(null);
    }

    public Task<IReadOnlyList<ISessionChain>> GetChainsByUserAsync(UserKey userKey)
    {
        if (!_roots.TryGetValue(userKey, out var root))
            return Task.FromResult<IReadOnlyList<ISessionChain>>(Array.Empty<ISessionChain>());

        return Task.FromResult<IReadOnlyList<ISessionChain>>(root.Chains.ToList());
    }

    public Task<IReadOnlyList<ISession>> GetSessionsByChainAsync(SessionChainId chainId)
    {
        var result = _sessions.Values
            .Where(s => s.ChainId == chainId)
            .ToList();

        return Task.FromResult<IReadOnlyList<ISession>>(result);
    }

    public Task DeleteExpiredSessionsAsync(DateTimeOffset at)
    {
        foreach (var kvp in _sessions)
        {
            var session = kvp.Value;

            if (session.ExpiresAt <= at)
            {
                _sessions[kvp.Key] = session.Revoke(at);
            }
        }

        return Task.CompletedTask;
    }
}
