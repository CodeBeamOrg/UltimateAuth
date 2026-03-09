using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
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

    public Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_sessions.TryGetValue(sessionId, out var s) ? s : null);
    }

    public Task SaveSessionAsync(UAuthSession session, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_sessions.TryGetValue(session.SessionId, out var current))
            throw new UAuthNotFoundException("session_not_found");

        if (current.Version != expectedVersion)
            throw new UAuthConcurrencyException("session_concurrency_conflict");

        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task CreateSessionAsync(UAuthSession session, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

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

    public Task<bool> RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_sessions.TryGetValue(sessionId, out var session))
            return Task.FromResult(false);

        if (session.IsRevoked)
            return Task.FromResult(false);

        var revoked = session.Revoke(at);
        _sessions[sessionId] = revoked;

        if (_chains.TryGetValue(session.ChainId, out var chain))
        {
            if (chain.ActiveSessionId == sessionId)
            {
                var updatedChain = chain.DetachSession(at);
                _chains[chain.ChainId] = updatedChain;
            }
        }

        return Task.FromResult(true);
    }

    public Task RevokeAllSessionsAsync(UserKey user, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            foreach (var (id, chain) in _chains)
            {
                if (chain.UserKey != user)
                    continue;

                var sessions = _sessions.Values.Where(s => s.ChainId == id).ToList();

                foreach (var session in sessions)
                {
                    if (!session.IsRevoked)
                    {
                        var revoked = session.Revoke(at);
                        _sessions[session.SessionId] = revoked;
                    }
                }

                if (chain.ActiveSessionId is not null)
                {
                    var updatedChain = chain.DetachSession(at);
                    _chains[id] = updatedChain;
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeOtherSessionsAsync(UserKey user, SessionChainId keepChain, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            foreach (var (id, chain) in _chains)
            {
                if (chain.UserKey != user)
                    continue;

                if (id == keepChain)
                    continue;

                var sessions = _sessions.Values.Where(s => s.ChainId == id).ToList();

                foreach (var session in sessions)
                {
                    if (!session.IsRevoked)
                    {
                        var revoked = session.Revoke(at);
                        _sessions[session.SessionId] = revoked;
                    }
                }

                if (chain.ActiveSessionId is not null)
                {
                    var updatedChain = chain.DetachSession(at);
                    _chains[id] = updatedChain;
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<UAuthSessionChain?> GetChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_chains.TryGetValue(chainId, out var c) ? c : null);
    }

    public Task SaveChainAsync(UAuthSessionChain chain, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_chains.TryGetValue(chain.ChainId, out var current))
            throw new UAuthNotFoundException("chain_not_found");

        if (current.Version != expectedVersion)
            throw new UAuthConcurrencyException("chain_concurrency_conflict");

        _chains[chain.ChainId] = chain;
        return Task.CompletedTask;
    }

    public Task CreateChainAsync(UAuthSessionChain chain, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

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

    public Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_chains.TryGetValue(chainId, out var chain))
        {
            _chains[chainId] = chain.Revoke(at);
        }
        return Task.CompletedTask;
    }

    public Task RevokeOtherChainsAsync(TenantKey tenant, UserKey user, SessionChainId keepChain, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var (id, chain) in _chains)
        {
            if (chain.Tenant != tenant)
                continue;

            if (chain.UserKey != user)
                continue;

            if (id == keepChain)
                continue;

            if (!chain.IsRevoked)
                _chains[id] = chain.Revoke(at);
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllChainsAsync(TenantKey tenant, UserKey user, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var (id, chain) in _chains)
        {
            if (chain.Tenant != tenant)
                continue;

            if (chain.UserKey != user)
                continue;

            if (!chain.IsRevoked)
                _chains[id] = chain.Revoke(at);
        }

        return Task.CompletedTask;
    }

    public Task<UAuthSessionRoot?> GetRootByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_roots.TryGetValue(userKey, out var r) ? r : null);
    }

    public Task<UAuthSessionRoot?> GetRootByIdAsync(SessionRootId rootId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_roots.Values.FirstOrDefault(r => r.RootId == rootId));
    }

    public Task SaveRootAsync(UAuthSessionRoot root, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_roots.TryGetValue(root.UserKey, out var current))
            throw new UAuthNotFoundException("root_not_found");

        if (current.Version != expectedVersion)
            throw new UAuthConcurrencyException("root_concurrency_conflict");

        _roots[root.UserKey] = root;
        return Task.CompletedTask;
    }

    public Task CreateRootAsync(UAuthSessionRoot root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

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

    public Task RevokeRootAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_roots.TryGetValue(userKey, out var root))
        {
            _roots[userKey] = root.Revoke(at);
        }
        return Task.CompletedTask;
    }

    public Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_sessions.TryGetValue(sessionId, out var session))
            return Task.FromResult<SessionChainId?>(session.ChainId);

        return Task.FromResult<SessionChainId?>(null);
    }

    public Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey,bool includeHistoricalRoots = false, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var roots = _roots.Values.Where(r => r.UserKey == userKey);

        if (!includeHistoricalRoots)
        {
            roots = roots.Where(r => !r.IsRevoked);
        }

        var rootIds = roots.Select(r => r.RootId).ToHashSet();

        var result = _chains.Values.Where(c => rootIds.Contains(c.RootId)).ToList().AsReadOnly();
        return Task.FromResult<IReadOnlyList<UAuthSessionChain>>(result);
    }

    public Task<IReadOnlyList<UAuthSessionChain>> GetChainsByRootAsync(SessionRootId rootId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = _chains.Values.Where(c => c.RootId == rootId).ToList().AsReadOnly();
        return Task.FromResult<IReadOnlyList<UAuthSessionChain>>(result);
    }

    public Task<UAuthSessionChain?> GetChainByDeviceAsync(TenantKey tenant, UserKey userKey, DeviceId deviceId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var chain = _chains.Values
            .FirstOrDefault(c =>
                c.Tenant == tenant &&
                c.UserKey == userKey &&
                !c.IsRevoked &&
                c.Device.DeviceId == deviceId);

        return Task.FromResult(chain);
    }

    public Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = _sessions.Values.Where(s => s.ChainId == chainId).ToList();
        return Task.FromResult<IReadOnlyList<UAuthSession>>(result);
    }

    public Task RemoveSessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return Task.CompletedTask;

            _sessions.TryRemove(sessionId, out _);

            if (_chains.TryGetValue(session.ChainId, out var chain))
            {
                if (chain.ActiveSessionId == sessionId)
                {
                    var updatedChain = chain.DetachSession(DateTimeOffset.UtcNow);
                    _chains[chain.ChainId] = updatedChain;
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeChainCascadeAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_chains.TryGetValue(chainId, out var chain))
                return Task.CompletedTask;

            if (!chain.IsRevoked)
            {
                var revokedChain = chain.Revoke(at);
                _chains[chainId] = revokedChain;
            }

            var sessions = _sessions.Values.Where(s => s.ChainId == chainId).ToList();

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

    public Task LogoutChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_chains.TryGetValue(chainId, out var chain))
                return Task.CompletedTask;

            if (chain.IsRevoked)
                return Task.CompletedTask;

            var sessions = _sessions.Values.Where(s => s.ChainId == chainId).ToList();

            foreach (var session in sessions)
            {
                if (!session.IsRevoked)
                {
                    var revokedSession = session.Revoke(at);
                    _sessions[session.SessionId] = revokedSession;
                }
            }

            if (chain.ActiveSessionId is not null)
            {
                var updatedChain = chain.DetachSession(at);
                _chains[chainId] = updatedChain;
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeRootCascadeAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

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
