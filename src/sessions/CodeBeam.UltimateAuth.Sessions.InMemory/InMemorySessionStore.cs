using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Sessions.InMemory;

internal sealed class InMemorySessionStore : ISessionStore
{
    private readonly TenantKey _tenant;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _execution = new(1, 1);
    private readonly AsyncLocal<Transaction?> _ambient = new();
    private State _committed = new();
    private long _revision;

    public InMemorySessionStore(TenantKey tenant) => _tenant = tenant;

    private sealed class State
    {
        public Dictionary<AuthSessionId, UAuthSession> Sessions { get; } = new();
        public Dictionary<SessionChainId, UAuthSessionChain> Chains { get; } = new();
        public Dictionary<SessionRootId, UAuthSessionRoot> Roots { get; } = new();

        public State Copy()
        {
            var result = new State();
            foreach (var item in Sessions) result.Sessions.Add(item.Key, item.Value);
            foreach (var item in Chains) result.Chains.Add(item.Key, item.Value);
            foreach (var item in Roots) result.Roots.Add(item.Key, item.Value);
            return result;
        }
    }

    private sealed class Transaction(State state, long revision)
    {
        public State State { get; } = state;
        public long Revision { get; } = revision;
        public bool Active { get; set; } = true;
    }

    private T Read<T>(Func<State, T> read)
    {
        var transaction = _ambient.Value;
        if (transaction is not null)
        {
            if (!transaction.Active)
                throw new InvalidOperationException("The session store transaction has completed.");
            return read(transaction.State);
        }

        lock (_sync)
            return read(_committed);
    }

    private T Write<T>(Func<State, T> write)
    {
        var transaction = _ambient.Value;
        if (transaction is not null)
        {
            if (!transaction.Active)
                throw new InvalidOperationException("The session store transaction has completed.");
            return write(transaction.State);
        }

        lock (_sync)
        {
            var result = write(_committed);
            _revision++;
            return result;
        }
    }

    private void Write(Action<State> write) => Write(state =>
    {
        write(state);
        return true;
    });

    private async Task<TResult> ExecuteTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_ambient.Value is not null)
            throw new InvalidOperationException("Nested session store transactions are not supported.");

        await _execution.WaitAsync(ct);
        Transaction? transaction = null;
        try
        {
            lock (_sync)
                transaction = new Transaction(_committed.Copy(), _revision);

            _ambient.Value = transaction;
            var result = await action(ct);
            ct.ThrowIfCancellationRequested();

            lock (_sync)
            {
                if (_revision != transaction.Revision)
                    throw new UAuthConcurrencyException("session_store_concurrency_conflict");

                _committed = transaction.State;
                _revision++;
            }

            return result;
        }
        finally
        {
            if (transaction is not null)
                transaction.Active = false;
            _ambient.Value = null;
            _execution.Release();
        }
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        await ExecuteTransactionAsync(async token =>
        {
            await action(token);
            return true;
        }, ct);
    }

    public Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
        ExecuteTransactionAsync(action, ct);

    public Task<TResult> ExecuteAdmissionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) =>
        ExecuteTransactionAsync(action, ct);

    public Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read(s => s.Sessions.GetValueOrDefault(sessionId)));
    }

    public Task SaveSessionAsync(UAuthSession session, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (!s.Sessions.TryGetValue(session.SessionId, out var current))
                throw new UAuthNotFoundException("session_not_found");
            if (current.Version != expectedVersion)
                throw new UAuthConcurrencyException("session_concurrency_conflict");
            s.Sessions[session.SessionId] = session;
        });
        return Task.CompletedTask;
    }

    public Task CreateSessionAsync(UAuthSession session, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (session.Tenant != _tenant)
                throw new InvalidOperationException("Tenant mismatch.");
            if (s.Sessions.ContainsKey(session.SessionId))
                throw new UAuthConcurrencyException("session_already_exists");
            if (session.Version != 0)
                throw new InvalidOperationException("New session must have version 0.");
            s.Sessions.Add(session.SessionId, session);
        });
        return Task.CompletedTask;
    }

    public Task<bool> RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Write(s =>
        {
            if (!s.Sessions.TryGetValue(sessionId, out var session) || session.IsRevoked)
                return false;
            s.Sessions[sessionId] = session.Revoke(at);
            if (s.Chains.TryGetValue(session.ChainId, out var chain) &&
                chain.ActiveSessionId == sessionId)
                s.Chains[chain.ChainId] = chain.DetachSession(at);
            return true;
        }));
    }

    public Task RevokeAllSessionsAsync(UserKey user, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            foreach (var (id, chain) in s.Chains.ToArray())
            {
                if (chain.Tenant != _tenant || chain.UserKey != user) continue;
                foreach (var session in s.Sessions.Values.Where(x => x.ChainId == id).ToArray())
                    if (!session.IsRevoked) s.Sessions[session.SessionId] = session.Revoke(at);
                if (chain.ActiveSessionId is not null)
                    s.Chains[id] = chain.DetachSession(at);
            }
        });
        return Task.CompletedTask;
    }

    public Task RevokeOtherSessionsAsync(UserKey user, SessionChainId keepChain, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (!s.Chains.TryGetValue(keepChain, out var preserved) ||
                preserved.Tenant != _tenant || preserved.UserKey != user)
                throw new UAuthNotFoundException("session_chain_not_found");
            foreach (var (id, chain) in s.Chains.ToArray())
            {
                if (chain.Tenant != _tenant || chain.UserKey != user || id == keepChain) continue;
                foreach (var session in s.Sessions.Values.Where(x => x.ChainId == id).ToArray())
                    if (!session.IsRevoked) s.Sessions[session.SessionId] = session.Revoke(at);
                if (chain.ActiveSessionId is not null)
                    s.Chains[id] = chain.DetachSession(at);
            }
        });
        return Task.CompletedTask;
    }

    public Task<UAuthSessionChain?> GetChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read(s => s.Chains.GetValueOrDefault(chainId)));
    }

    public Task SaveChainAsync(UAuthSessionChain chain, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (!s.Chains.TryGetValue(chain.ChainId, out var current))
                throw new UAuthNotFoundException("chain_not_found");
            if (current.Version != expectedVersion)
                throw new UAuthConcurrencyException("chain_concurrency_conflict");
            s.Chains[chain.ChainId] = chain;
        });
        return Task.CompletedTask;
    }

    public Task CreateChainAsync(UAuthSessionChain chain, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (chain.Tenant != _tenant)
                throw new InvalidOperationException("Tenant mismatch.");
            if (s.Chains.ContainsKey(chain.ChainId))
                throw new UAuthConcurrencyException("chain_already_exists");
            if (chain.Version != 0)
                throw new InvalidOperationException("New chain must have version 0.");
            s.Chains.Add(chain.ChainId, chain);
        });
        return Task.CompletedTask;
    }

    public Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (s.Chains.TryGetValue(chainId, out var chain))
                s.Chains[chainId] = chain.Revoke(at);
        });
        return Task.CompletedTask;
    }

    public Task RevokeOtherChainsAsync(UserKey user, SessionChainId keepChain, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            foreach (var (id, chain) in s.Chains.ToArray())
                if (chain.Tenant == _tenant && chain.UserKey == user && id != keepChain && !chain.IsRevoked)
                    s.Chains[id] = chain.Revoke(at);
        });
        return Task.CompletedTask;
    }

    public Task RevokeAllChainsAsync(UserKey user, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            foreach (var (id, chain) in s.Chains.ToArray())
                if (chain.Tenant == _tenant && chain.UserKey == user && !chain.IsRevoked)
                    s.Chains[id] = chain.Revoke(at);
        });
        return Task.CompletedTask;
    }

    public Task<UAuthSessionRoot?> GetActiveRootByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read(s => s.Roots.Values.SingleOrDefault(
            x => x.Tenant == _tenant && x.UserKey == userKey && !x.IsRevoked)));
    }

    public Task<UAuthSessionRoot?> GetRootByIdAsync(SessionRootId rootId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read(s =>
            s.Roots.TryGetValue(rootId, out var root) && root.Tenant == _tenant ? root : null));
    }

    public Task SaveRootAsync(UAuthSessionRoot root, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (root.Tenant != _tenant)
                throw new UAuthValidationException("Tenant mismatch.");
            if (!s.Roots.TryGetValue(root.RootId, out var current))
                throw new UAuthNotFoundException("root_not_found");
            if (current.Version != expectedVersion)
                throw new UAuthConcurrencyException("root_concurrency_conflict");
            s.Roots[root.RootId] = root;
        });
        return Task.CompletedTask;
    }

    public Task CreateRootAsync(UAuthSessionRoot root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (root.Tenant != _tenant)
                throw new UAuthValidationException("Tenant mismatch.");
            if (root.Version != 0)
                throw new UAuthValidationException("New root must have version 0.");
            if (root.IsRevoked)
                throw new UAuthValidationException("New root cannot already be revoked.");
            if (s.Roots.ContainsKey(root.RootId))
                throw new UAuthConflictException("session_root_already_exists");
            if (s.Roots.Values.Any(x => x.Tenant == _tenant && x.UserKey == root.UserKey && !x.IsRevoked))
                throw new UAuthConflictException("active_session_root_already_exists");
            s.Roots.Add(root.RootId, root);
        });
        return Task.CompletedTask;
    }

    public Task RevokeRootAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            var root = s.Roots.Values.SingleOrDefault(
                x => x.Tenant == _tenant && x.UserKey == userKey && !x.IsRevoked);
            if (root is not null) s.Roots[root.RootId] = root.Revoke(at);
        });
        return Task.CompletedTask;
    }

    public Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read(s =>
            s.Sessions.TryGetValue(sessionId, out var session)
                ? (SessionChainId?)session.ChainId : null));
    }

    public Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(
        UserKey userKey, bool includeHistoricalRoots = false, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read<IReadOnlyList<UAuthSessionChain>>(s =>
        {
            var roots = s.Roots.Values.Where(x => x.Tenant == _tenant && x.UserKey == userKey);
            if (!includeHistoricalRoots) roots = roots.Where(x => !x.IsRevoked);
            var rootIds = roots.Select(x => x.RootId).ToHashSet();
            return s.Chains.Values
                .Where(x => x.Tenant == _tenant && rootIds.Contains(x.RootId)).ToArray();
        }));
    }

    public Task<IReadOnlyList<UAuthSessionChain>> GetChainsByRootAsync(SessionRootId rootId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read<IReadOnlyList<UAuthSessionChain>>(s =>
            s.Chains.Values.Where(x => x.Tenant == _tenant && x.RootId == rootId).ToArray()));
    }

    public Task<UAuthSessionChain?> GetChainByDeviceAsync(UserKey userKey, DeviceId deviceId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read(s => s.Chains.Values.FirstOrDefault(x =>
            x.Tenant == _tenant && x.UserKey == userKey &&
            !x.IsRevoked && x.Device.DeviceId == deviceId)));
    }

    public Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read<IReadOnlyList<UAuthSession>>(s =>
            s.Sessions.Values.Where(x => x.Tenant == _tenant && x.ChainId == chainId).ToArray()));
    }

    public Task RemoveSessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (!s.Sessions.TryGetValue(sessionId, out var session)) return;
            s.Sessions.Remove(sessionId);
            if (s.Chains.TryGetValue(session.ChainId, out var chain) &&
                chain.ActiveSessionId == sessionId)
                s.Chains[chain.ChainId] = chain.DetachSession(DateTimeOffset.UtcNow);
        });
        return Task.CompletedTask;
    }

    public Task RevokeChainCascadeAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (!s.Chains.TryGetValue(chainId, out var chain)) return;
            if (!chain.IsRevoked) s.Chains[chainId] = chain.Revoke(at);
            foreach (var session in s.Sessions.Values.Where(x => x.ChainId == chainId).ToArray())
                if (!session.IsRevoked) s.Sessions[session.SessionId] = session.Revoke(at);
        });
        return Task.CompletedTask;
    }

    public Task LogoutChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            if (!s.Chains.TryGetValue(chainId, out var chain) || chain.IsRevoked) return;
            foreach (var session in s.Sessions.Values.Where(x => x.ChainId == chainId).ToArray())
                if (!session.IsRevoked) s.Sessions[session.SessionId] = session.Revoke(at);
            if (chain.ActiveSessionId is not null)
                s.Chains[chainId] = chain.DetachSession(at);
        });
        return Task.CompletedTask;
    }

    public Task RevokeRootCascadeAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Write(s =>
        {
            var root = s.Roots.Values.SingleOrDefault(x =>
                x.Tenant == _tenant && x.UserKey == userKey && !x.IsRevoked);
            if (root is null) return;
            foreach (var (id, chain) in s.Chains.ToArray())
            {
                if (chain.Tenant != _tenant || chain.UserKey != userKey || chain.RootId != root.RootId)
                    continue;
                if (!chain.IsRevoked) s.Chains[id] = chain.Revoke(at);
                foreach (var session in s.Sessions.Values.Where(x => x.ChainId == id).ToArray())
                    if (!session.IsRevoked) s.Sessions[session.SessionId] = session.Revoke(at);
            }
            s.Roots[root.RootId] = root.Revoke(at);
        });
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<SessionChainId, int>> GetSessionCountsByChainsAsync(
        IReadOnlyCollection<SessionChainId> chainIds, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Read<IReadOnlyDictionary<SessionChainId, int>>(s =>
        {
            var ids = chainIds.ToHashSet();
            return s.Sessions.Values.Where(x => x.Tenant == _tenant && ids.Contains(x.ChainId))
                .GroupBy(x => x.ChainId)
                .ToDictionary(x => x.Key, x => x.Count());
        }));
    }
}
