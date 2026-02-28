using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Sessions.InMemory;

internal sealed class InMemorySessionStore : ISessionStore
{
    private readonly SemaphoreSlim _tx = new(1, 1);
    private readonly object _lock = new();

    private readonly ConcurrentDictionary<AuthSessionId, UAuthSession> _sessions = new();
    private readonly ConcurrentDictionary<SessionChainId, UAuthSessionChain> _chains = new();
    private readonly ConcurrentDictionary<UserKey, UAuthSessionRoot> _roots = new();

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

    public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
    {
        await _tx.WaitAsync(ct);
        try
        {
            return await action(ct);
        }
        finally
        {
            _tx.Release();
        }
    }

    public Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId)
        => Task.FromResult(_sessions.TryGetValue(sessionId, out var s) ? s : null);

    public Task SaveSessionAsync(UAuthSession session, long expectedVersion)
    {
        if (!_sessions.TryGetValue(session.SessionId, out var current))
            throw new UAuthNotFoundException("session_not_found");

        if (current.Version != expectedVersion)
            throw new UAuthConcurrencyException("session_concurrency_conflict");

        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task CreateSessionAsync(UAuthSession session)
    {
        lock (_lock)
        {
            if (_sessions.ContainsKey(session.SessionId))
                throw new UAuthConcurrencyException("session_already_exists");

            if (session.Version != 0)
                throw new InvalidOperationException("New session must have version 0.");

            _sessions[session.SessionId] = session;
        }

        return Task.CompletedTask;
    }

    public Task<bool> RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return Task.FromResult(false);

        if (session.IsRevoked)
            return Task.FromResult(false);

        _sessions[sessionId] = session.Revoke(at);
        return Task.FromResult(true);
    }

    public Task<UAuthSessionChain?> GetChainAsync(SessionChainId chainId)
        => Task.FromResult(_chains.TryGetValue(chainId, out var c) ? c : null);

    public Task SaveChainAsync(UAuthSessionChain chain, long expectedVersion)
    {
        if (!_chains.TryGetValue(chain.ChainId, out var current))
            throw new UAuthNotFoundException("chain_not_found");

        if (current.Version != expectedVersion)
            throw new UAuthConcurrencyException("chain_concurrency_conflict");

        _chains[chain.ChainId] = chain;
        return Task.CompletedTask;
    }

    public Task CreateChainAsync(UAuthSessionChain chain)
    {
        lock (_lock)
        {
            if (_chains.ContainsKey(chain.ChainId))
                throw new UAuthConcurrencyException("chain_already_exists");

            if (chain.Version != 0)
                throw new InvalidOperationException("New chain must have version 0.");

            _chains[chain.ChainId] = chain;
        }

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

    //public Task<AuthSessionId?> GetActiveSessionIdAsync(SessionChainId chainId)
    //    => Task.FromResult<AuthSessionId?>(_activeSessions.TryGetValue(chainId, out var id) ? id : null);

    //public Task SetActiveSessionIdAsync(SessionChainId chainId, AuthSessionId sessionId)
    //{
    //    _activeSessions[chainId] = sessionId;
    //    return Task.CompletedTask;
    //}

    public Task<UAuthSessionRoot?> GetRootByUserAsync(UserKey userKey)
        => Task.FromResult(_roots.TryGetValue(userKey, out var r) ? r : null);

    public Task<UAuthSessionRoot?> GetRootByIdAsync(SessionRootId rootId)
        => Task.FromResult(_roots.Values.FirstOrDefault(r => r.RootId == rootId));

    public Task SaveRootAsync(UAuthSessionRoot root, long expectedVersion)
    {
        if (!_roots.TryGetValue(root.UserKey, out var current))
            throw new UAuthNotFoundException("root_not_found");

        if (current.Version != expectedVersion)
            throw new UAuthConcurrencyException("root_concurrency_conflict");

        _roots[root.UserKey] = root;
        return Task.CompletedTask;
    }

    public Task CreateRootAsync(UAuthSessionRoot root)
    {
        lock (_lock)
        {
            if (_roots.ContainsKey(root.UserKey))
                throw new UAuthConcurrencyException("root_already_exists");

            if (root.Version != 0)
                throw new InvalidOperationException("New root must have version 0.");

            _roots[root.UserKey] = root;
        }

        return Task.CompletedTask;
    }

    public Task RevokeRootAsync(UserKey userKey, DateTimeOffset at)
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

    public Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey)
    {
        if (!_roots.TryGetValue(userKey, out var root))
            return Task.FromResult<IReadOnlyList<UAuthSessionChain>>(Array.Empty<UAuthSessionChain>());

        return Task.FromResult<IReadOnlyList<UAuthSessionChain>>(root.Chains.ToList());
    }

    public Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId)
    {
        var result = _sessions.Values
            .Where(s => s.ChainId == chainId)
            .ToList();

        return Task.FromResult<IReadOnlyList<UAuthSession>>(result);
    }

    public Task DeleteExpiredSessionsAsync(DateTimeOffset at)
    {
        foreach (var kvp in _sessions)
        {
            var session = kvp.Value;

            if (session.ExpiresAt <= at)
            {
                var revoked = session.Revoke(at);
                _sessions[kvp.Key] = revoked;

                //if (_activeSessions.TryGetValue(revoked.ChainId, out var activeId) && activeId == revoked.SessionId)
                //{
                //    _activeSessions.TryRemove(revoked.ChainId, out _);
                //}
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeChainCascadeAsync(SessionChainId chainId, DateTimeOffset at)
    {
        lock (_lock)
        {
            if (!_chains.TryGetValue(chainId, out var chain))
                return Task.CompletedTask;

            if (!chain.IsRevoked)
            {
                var revokedChain = chain.Revoke(at);
                _chains[chainId] = revokedChain;
            }

            var sessions = _sessions.Values
                .Where(s => s.ChainId == chainId)
                .ToList();

            foreach (var session in sessions)
            {
                if (!session.IsRevoked)
                {
                    var revokedSession = session.Revoke(at);
                    _sessions[session.SessionId] = revokedSession;
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeRootCascadeAsync(UserKey userKey, DateTimeOffset at)
    {
        lock (_lock)
        {
            if (!_roots.TryGetValue(userKey, out var root))
                return Task.CompletedTask;

            var chains = _chains.Values
                .Where(c => c.UserKey == userKey && c.Tenant == root.Tenant)
                .ToList();

            foreach (var chain in chains)
            {
                if (!chain.IsRevoked)
                {
                    var revokedChain = chain.Revoke(at);
                    _chains[chain.ChainId] = revokedChain;
                }

                var sessions = _sessions.Values
                    .Where(s => s.ChainId == chain.ChainId)
                    .ToList();

                foreach (var session in sessions)
                {
                    if (!session.IsRevoked)
                    {
                        var revokedSession = session.Revoke(at);
                        _sessions[session.SessionId] = revokedSession;
                    }
                }
            }

            if (!root.IsRevoked)
            {
                var revokedRoot = root.Revoke(at);
                _roots[userKey] = revokedRoot;
            }
        }

        return Task.CompletedTask;
    }
}
