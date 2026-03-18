using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class EfCoreSessionStore : ISessionStore
{
    private readonly UltimateAuthSessionDbContext _db;
    private readonly TenantKey _tenant;

    public EfCoreSessionStore(UltimateAuthSessionDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

            try
            {
                await action(ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                throw new UAuthConcurrencyException("concurrency_conflict");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

            try
            {
                var result = await action(ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                throw new UAuthConcurrencyException("concurrency_conflict");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Sessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.SessionId == sessionId);

        return projection?.ToDomain();
    }

    public Task SaveSessionAsync(UAuthSession session, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = session.ToProjection();
        _db.Sessions.Attach(projection);
        _db.Entry(projection).Property(x => x.Version).OriginalValue = expectedVersion;
        _db.Entry(projection).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task CreateSessionAsync(UAuthSession session, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = session.ToProjection();

        if (session.Version != 0)
            throw new InvalidOperationException("New session must have version 0.");

        _db.Sessions.Add(projection);

        return Task.CompletedTask;
    }

    public async Task<bool> RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Sessions.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.SessionId == sessionId, ct);

        if (projection is null || projection.IsRevoked)
            return false;

        var revoked = projection.ToDomain().Revoke(at);
        _db.Sessions.Update(revoked.ToProjection());
        return true;
    }

    public async Task RevokeAllSessionsAsync(UserKey user, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var chains = await _db.Chains.Where(x => x.Tenant == _tenant && x.UserKey == user).ToListAsync(ct);
        var chainIds = chains.Select(x => x.ChainId).ToList();
        var sessions = await _db.Sessions.Where(x => x.Tenant == _tenant && chainIds.Contains(x.ChainId)).ToListAsync(ct);

        foreach (var sessionProjection in sessions)
        {
            var session = sessionProjection.ToDomain();

            if (!session.IsRevoked)
                _db.Sessions.Update(session.Revoke(at).ToProjection());
        }

        foreach (var chainProjection in chains)
        {
            var chain = chainProjection.ToDomain();

            if (chain.ActiveSessionId is not null)
                _db.Chains.Update(chain.DetachSession(at).ToProjection());
        }
    }

    public async Task RevokeOtherSessionsAsync(UserKey user, SessionChainId keepChain, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var chains = await _db.Chains.Where(x => x.Tenant == _tenant && x.UserKey == user && x.ChainId != keepChain).ToListAsync(ct);
        var chainIds = chains.Select(x => x.ChainId).ToList();
        var sessions = await _db.Sessions.Where(x => x.Tenant == _tenant && chainIds.Contains(x.ChainId)).ToListAsync(ct);

        foreach (var sessionProjection in sessions)
        {
            var session = sessionProjection.ToDomain();

            if (!session.IsRevoked)
                _db.Sessions.Update(session.Revoke(at).ToProjection());
        }

        foreach (var chainProjection in chains)
        {
            var chain = chainProjection.ToDomain();

            if (chain.ActiveSessionId is not null)
                _db.Chains.Update(chain.DetachSession(at).ToProjection());
        }
    }

    public async Task<UAuthSessionChain?> GetChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Chains
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId);

        return projection?.ToDomain();
    }

    public async Task<UAuthSessionChain?> GetChainByDeviceAsync(UserKey userKey, DeviceId deviceId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Chains
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.RevokedAt == null &&
                x.DeviceId == deviceId)
            .SingleOrDefaultAsync(ct);

        return projection?.ToDomain();
    }

    public Task SaveChainAsync(UAuthSessionChain chain, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = chain.ToProjection();

        if (chain.Version != expectedVersion + 1)
            throw new InvalidOperationException("Chain version must be incremented by domain.");

        _db.Entry(projection).State = EntityState.Modified;

        _db.Entry(projection)
            .Property(x => x.Version)
            .OriginalValue = expectedVersion;

        return Task.CompletedTask;
    }

    public Task CreateChainAsync(UAuthSessionChain chain, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (chain.Version != 0)
            throw new InvalidOperationException("New chain must have version 0.");

        var projection = chain.ToProjection();

        _db.Chains.Add(projection);

        return Task.CompletedTask;
    }

    public async Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Chains.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId);

        if (projection is null)
            return;

        var chain = projection.ToDomain();
        if (chain.IsRevoked)
            return;

        _db.Chains.Update(chain.Revoke(at).ToProjection());
    }

    public async Task LogoutChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var chainProjection = await _db.Chains.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId, ct);

        if (chainProjection is null)
            return;

        var chain = chainProjection.ToDomain();

        if (chain.IsRevoked)
            return;

        var sessions = await _db.Sessions.Where(x => x.Tenant == _tenant && x.ChainId == chainId).ToListAsync(ct);

        foreach (var sessionProjection in sessions)
        {
            var session = sessionProjection.ToDomain();

            if (session.IsRevoked)
                continue;

            var revoked = session.Revoke(at);
            _db.Sessions.Update(revoked.ToProjection());
        }

        if (chain.ActiveSessionId is not null)
        {
            var updatedChain = chain.DetachSession(at);
            _db.Chains.Update(updatedChain.ToProjection());
        }
    }

    public async Task RevokeOtherChainsAsync(UserKey userKey, SessionChainId currentChainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await _db.Chains
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.ChainId != currentChainId &&
                !x.IsRevoked)
            .ToListAsync(ct);

        foreach (var projection in projections)
        {
            var chain = projection.ToDomain();

            if (chain.IsRevoked)
                continue;

            _db.Chains.Update(chain.Revoke(at).ToProjection());
        }
    }

    public async Task RevokeAllChainsAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await _db.Chains
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                !x.IsRevoked)
            .ToListAsync(ct);

        foreach (var projection in projections)
        {
            var chain = projection.ToDomain();

            if (chain.IsRevoked)
                continue;

            _db.Chains.Update(chain.Revoke(at).ToProjection());
        }
    }

    public async Task<AuthSessionId?> GetActiveSessionIdAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await _db.Chains
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant && x.ChainId == chainId)
            .Select(x => x.ActiveSessionId)
            .SingleOrDefaultAsync();
    }

    public async Task SetActiveSessionIdAsync(SessionChainId chainId, AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Chains.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId);

        if (projection is null)
            return;

        projection.ActiveSessionId = sessionId;
        _db.Chains.Update(projection);
    }

    public async Task<UAuthSessionRoot?> GetRootByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var rootProjection = await _db.Roots.AsNoTracking().SingleOrDefaultAsync(x => x.Tenant == _tenant && x.UserKey == userKey, ct);
        return rootProjection?.ToDomain();
    }

    public Task SaveRootAsync(UAuthSessionRoot root, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = root.ToProjection();

        if (root.Version != expectedVersion + 1)
            throw new InvalidOperationException("Root version must be incremented by domain.");

        _db.Entry(projection).State = EntityState.Modified;

        _db.Entry(projection)
            .Property(x => x.Version)
            .OriginalValue = expectedVersion;

        return Task.CompletedTask;
    }

    public Task CreateRootAsync(UAuthSessionRoot root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (root.Version != 0)
            throw new InvalidOperationException("New root must have version 0.");

        var projection = root.ToProjection();

        _db.Roots.Add(projection);

        return Task.CompletedTask;
    }

    public async Task RevokeRootAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Roots.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.UserKey == userKey);

        if (projection is null)
            return;

        var root = projection.ToDomain();
        _db.Roots.Update(root.Revoke(at).ToProjection());
    }

    public async Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await _db.Sessions
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant && x.SessionId == sessionId)
            .Select(x => (SessionChainId?)x.ChainId)
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey, bool includeHistoricalRoots = false, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var rootsQuery = _db.Roots.AsNoTracking().Where(x => x.Tenant == _tenant && x.UserKey == userKey);

        if (!includeHistoricalRoots)
        {
            rootsQuery = rootsQuery.Where(r => !r.IsRevoked);
        }

        var rootIds = await rootsQuery.Select(r => r.RootId).ToListAsync();

        if (rootIds.Count == 0)
            return Array.Empty<UAuthSessionChain>();

        var projections = await _db.Chains.AsNoTracking().Where(x => x.Tenant == _tenant && rootIds.Contains(x.RootId)).ToListAsync();
        return projections.Select(c => c.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<UAuthSessionChain>> GetChainsByRootAsync(SessionRootId rootId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await _db.Chains
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant && x.RootId == rootId)
            .ToListAsync();

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await _db.Sessions
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant && x.ChainId == chainId)
            .ToListAsync();

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<UAuthSessionRoot?> GetRootByIdAsync(SessionRootId rootId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Roots.AsNoTracking().SingleOrDefaultAsync(x => x.Tenant == _tenant && x.RootId == rootId, ct);
        return projection?.ToDomain();
    }

    public async Task RemoveSessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Sessions.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.SessionId == sessionId, ct);

        if (projection is null)
            return;

        _db.Sessions.Remove(projection);
    }

    public async Task RevokeChainCascadeAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var chainProjection = await _db.Chains
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId);

        if (chainProjection is null)
            return;

        var sessionProjections = await _db.Sessions.Where(x => x.Tenant == _tenant && x.ChainId == chainId && !x.IsRevoked).ToListAsync();

        foreach (var sessionProjection in sessionProjections)
        {
            var session = sessionProjection.ToDomain();
            var revoked = session.Revoke(at);

            _db.Sessions.Update(revoked.ToProjection());
        }

        if (!chainProjection.IsRevoked)
        {
            var chain = chainProjection.ToDomain();
            var revokedChain = chain.Revoke(at);

            _db.Chains.Update(revokedChain.ToProjection());
        }
    }

    public async Task RevokeRootCascadeAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var rootProjection = await _db.Roots.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.UserKey == userKey);

        if (rootProjection is null)
            return;

        var chainProjections = await _db.Chains.Where(x => x.Tenant == _tenant && x.UserKey == userKey).ToListAsync();

        foreach (var chainProjection in chainProjections)
        {
            var chainId = chainProjection.ChainId;

            var sessionProjections = await _db.Sessions.Where(x => x.Tenant == _tenant && x.ChainId == chainId && !x.IsRevoked).ToListAsync();

            foreach (var sessionProjection in sessionProjections)
            {
                var session = sessionProjection.ToDomain();
                var revokedSession = session.Revoke(at);

                _db.Sessions.Update(revokedSession.ToProjection());
            }

            if (!chainProjection.IsRevoked)
            {
                var chain = chainProjection.ToDomain();
                var revokedChain = chain.Revoke(at);

                _db.Chains.Update(revokedChain.ToProjection());
            }
        }

        if (!rootProjection.IsRevoked)
        {
            var root = rootProjection.ToDomain();
            var revokedRoot = root.Revoke(at);

            _db.Roots.Update(revokedRoot.ToProjection());
        }
    }
}
