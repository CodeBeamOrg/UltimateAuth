using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class EfCoreSessionStore<TDbContext> : ISessionStore where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly TenantKey _tenant;

    public EfCoreSessionStore(TDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    private DbSet<SessionProjection> DbSetSession => _db.Set<SessionProjection>();
    private DbSet<SessionChainProjection> DbSetChain => _db.Set<SessionChainProjection>();
    private DbSet<SessionRootProjection> DbSetRoot => _db.Set<SessionRootProjection>();

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

        var local = DbSetSession.Local.FirstOrDefault(x => x.Tenant == _tenant && x.SessionId == sessionId);

        if (local != null)
            return local.ToDomain();

        var projection = await DbSetSession
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.SessionId == sessionId);

        return projection?.ToDomain();
    }

    public async Task SaveSessionAsync(UAuthSession session, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = DbSetSession.Local.FirstOrDefault(x => x.Tenant == _tenant && x.SessionId == session.SessionId);

        if (projection == null)
        {
            projection = await DbSetSession
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.SessionId == session.SessionId,
                ct);
        }

        if (projection is null)
            throw new UAuthNotFoundException("session_not_found");

        if (projection.Version != expectedVersion)
            throw new UAuthConcurrencyException("session_concurrency_conflict");

        session.UpdateProjection(projection);
        projection.Version++;
    }

    public Task CreateSessionAsync(UAuthSession session, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = session.ToProjection();

        if (session.Version != 0)
            throw new InvalidOperationException("New session must have version 0.");

        DbSetSession.Add(projection);

        return Task.CompletedTask;
    }

    public async Task<bool> RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSetSession.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.SessionId == sessionId, ct);

        if (projection is null || projection.RevokedAt is not null)
            return false;

        var domain = projection.ToDomain().Revoke(at);
        domain.UpdateProjection(projection);
        projection.Version++;

        return true;
    }

    public async Task RevokeAllSessionsAsync(UserKey user, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var chains = await DbSetChain
            .Where(x => x.Tenant == _tenant && x.UserKey == user)
            .ToListAsync(ct);

        var chainIds = chains.Select(x => x.ChainId).ToList();

        var sessions = await DbSetSession
            .Where(x => x.Tenant == _tenant && chainIds.Contains(x.ChainId))
            .ToListAsync(ct);

        foreach (var sessionProjection in sessions)
        {
            if (sessionProjection.RevokedAt is not null)
                continue;

            var domain = sessionProjection.ToDomain().Revoke(at);
            domain.UpdateProjection(sessionProjection);
            sessionProjection.Version++;
        }

        foreach (var chainProjection in chains)
        {
            if (chainProjection.ActiveSessionId is null)
                continue;

            var domain = chainProjection.ToDomain().DetachSession(at);

            domain.UpdateProjection(chainProjection);
            chainProjection.Version++;
        }
    }

    public async Task RevokeOtherSessionsAsync(UserKey user, SessionChainId keepChain, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var chains = await DbSetChain
            .Where(x => x.Tenant == _tenant && x.UserKey == user && x.ChainId != keepChain)
            .ToListAsync(ct);

        var chainIds = chains.Select(x => x.ChainId).ToList();

        var sessions = await DbSetSession
            .Where(x => x.Tenant == _tenant && chainIds.Contains(x.ChainId))
            .ToListAsync(ct);

        foreach (var sessionProjection in sessions)
        {
            if (sessionProjection.RevokedAt is not null)
                continue;

            var domain = sessionProjection.ToDomain().Revoke(at);
            domain.UpdateProjection(sessionProjection);
            sessionProjection.Version++;
        }

        foreach (var chainProjection in chains)
        {
            if (chainProjection.ActiveSessionId is null)
                continue;

            var domain = chainProjection.ToDomain().DetachSession(at);

            domain.UpdateProjection(chainProjection);
            chainProjection.Version++;
        }
    }

    public async Task<UAuthSessionChain?> GetChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var local = DbSetChain.Local.FirstOrDefault(x => x.Tenant == _tenant && x.ChainId == chainId);

        if (local is not null)
            return local.ToDomain();

        var projection = await DbSetChain
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId, ct);

        return projection?.ToDomain();
    }

    public async Task<UAuthSessionChain?> GetChainByDeviceAsync(UserKey userKey, DeviceId deviceId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var local = DbSetChain.Local
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.RevokedAt == null &&
                x.DeviceId == deviceId)
            .FirstOrDefault();

        if (local != null)
            return local.ToDomain();

        var projection = await DbSetChain
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.RevokedAt == null &&
                x.DeviceId == deviceId)
            .FirstOrDefaultAsync(ct);

        return projection?.ToDomain();
    }

    public async Task SaveChainAsync(UAuthSessionChain chain, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = DbSetChain.Local.FirstOrDefault(x => x.Tenant == _tenant && x.ChainId == chain.ChainId);

        if (projection is null)
        {
            projection = await DbSetChain
                .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chain.ChainId, ct);
        }

        if (projection is null)
            throw new UAuthNotFoundException("chain_not_found");

        if (projection.Version != expectedVersion)
            throw new UAuthConcurrencyException("chain_concurrency_conflict");

        chain.UpdateProjection(projection);
        projection.Version++;
    }

    public Task CreateChainAsync(UAuthSessionChain chain, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (chain.Version != 0)
            throw new InvalidOperationException("New chain must have version 0.");

        var projection = chain.ToProjection();

        DbSetChain.Add(projection);
        _db.Entry(projection).State = EntityState.Added;

        return Task.CompletedTask;
    }

    public async Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSetChain
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId, ct);

        if (projection is null || projection.RevokedAt is not null)
            return;

        var domain = projection.ToDomain().Revoke(at);
        domain.UpdateProjection(projection);
        projection.Version++;
    }

    public async Task LogoutChainAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var chainProjection = await DbSetChain
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId, ct);

        if (chainProjection is null || chainProjection.RevokedAt is not null)
            return;

        var sessions = await DbSetSession
            .Where(x => x.Tenant == _tenant && x.ChainId == chainId)
            .ToListAsync(ct);

        foreach (var sessionProjection in sessions)
        {
            if (sessionProjection.RevokedAt is not null)
                continue;

            var domain = sessionProjection.ToDomain().Revoke(at);

            domain.UpdateProjection(sessionProjection);
            sessionProjection.Version++;
        }

        if (chainProjection.ActiveSessionId is not null)
        {
            var domain = chainProjection.ToDomain().DetachSession(at);
            domain.UpdateProjection(chainProjection);
            chainProjection.Version++;
        }
    }

    public async Task RevokeOtherChainsAsync(UserKey userKey, SessionChainId currentChainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await DbSetChain
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.ChainId != currentChainId &&
                x.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var projection in projections)
        {
            var domain = projection.ToDomain().Revoke(at);
            domain.UpdateProjection(projection);
            projection.Version++;
        }
    }

    public async Task RevokeAllChainsAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await DbSetChain
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var projection in projections)
        {
            var domain = projection.ToDomain().Revoke(at);
            domain.UpdateProjection(projection);
            projection.Version++;
        }
    }

    public async Task<AuthSessionId?> GetActiveSessionIdAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await DbSetChain
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant && x.ChainId == chainId)
            .Select(x => x.ActiveSessionId)
            .SingleOrDefaultAsync();
    }

    public async Task SetActiveSessionIdAsync(SessionChainId chainId, AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = DbSetChain.Local.FirstOrDefault(x => x.Tenant == _tenant && x.ChainId == chainId);

        if (projection is null)
        {
            projection = await DbSetChain.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId, ct);
        }

        if (projection is null)
            throw new UAuthNotFoundException("chain_not_found");

        projection.ActiveSessionId = sessionId;
        projection.Version++;
    }

    public async Task<UAuthSessionRoot?> GetRootByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var rootProjection = await DbSetRoot.AsNoTracking().SingleOrDefaultAsync(x => x.Tenant == _tenant && x.UserKey == userKey, ct);
        return rootProjection?.ToDomain();
    }

    public async Task SaveRootAsync(UAuthSessionRoot root, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSetRoot
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.UserKey == root.UserKey,
                ct);

        if (projection is null)
            throw new UAuthNotFoundException("root_not_found");

        if (projection.Version != expectedVersion)
            throw new UAuthConcurrencyException("root_concurrency_conflict");

        root.UpdateProjection(projection);
        projection.Version++;
    }

    public Task CreateRootAsync(UAuthSessionRoot root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (root.Version != 0)
            throw new InvalidOperationException("New root must have version 0.");

        var projection = root.ToProjection();

        DbSetRoot.Add(projection);

        return Task.CompletedTask;
    }

    public async Task RevokeRootAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSetRoot
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.UserKey == userKey, ct);

        if (projection is null || projection.RevokedAt is not null)
            return;

        var domain = projection.ToDomain().Revoke(at);
        domain.UpdateProjection(projection);
        projection.Version++;
    }

    public async Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await DbSetSession
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant && x.SessionId == sessionId)
            .Select(x => (SessionChainId?)x.ChainId)
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey, bool includeHistoricalRoots = false, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var rootsQuery = DbSetRoot.AsNoTracking().Where(x => x.Tenant == _tenant && x.UserKey == userKey);

        if (!includeHistoricalRoots)
        {
            rootsQuery = rootsQuery.Where(x => x.RevokedAt == null);
        }

        var rootIds = await rootsQuery.Select(r => r.RootId).ToListAsync();

        if (rootIds.Count == 0)
            return Array.Empty<UAuthSessionChain>();

        var projections = await DbSetChain.AsNoTracking().Where(x => x.Tenant == _tenant && rootIds.Contains(x.RootId)).ToListAsync();
        return projections.Select(c => c.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<UAuthSessionChain>> GetChainsByRootAsync(SessionRootId rootId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await DbSetChain
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant && x.RootId == rootId)
            .ToListAsync();

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await DbSetSession
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant && x.ChainId == chainId)
            .ToListAsync();

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<UAuthSessionRoot?> GetRootByIdAsync(SessionRootId rootId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSetRoot.AsNoTracking().SingleOrDefaultAsync(x => x.Tenant == _tenant && x.RootId == rootId, ct);
        return projection?.ToDomain();
    }

    public async Task RemoveSessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSetSession.SingleOrDefaultAsync(x => x.Tenant == _tenant && x.SessionId == sessionId, ct);

        if (projection is null)
            return;

        DbSetSession.Remove(projection);
    }

    public async Task RevokeChainCascadeAsync(SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var chainProjection = await DbSetChain
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.ChainId == chainId, ct);

        if (chainProjection is null)
            return;

        var sessionProjections = await DbSetSession
            .Where(x => x.Tenant == _tenant && x.ChainId == chainId && x.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var sessionProjection in sessionProjections)
        {
            var revoked = sessionProjection.ToDomain().Revoke(at);
            revoked.UpdateProjection(sessionProjection);
            sessionProjection.Version++;
        }

        if (chainProjection.RevokedAt is null)
        {
            var revokedChain = chainProjection.ToDomain().Revoke(at);
            revokedChain.UpdateProjection(chainProjection);
            chainProjection.Version++;
        }
    }

    public async Task RevokeRootCascadeAsync(UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var rootProjection = await DbSetRoot
            .SingleOrDefaultAsync(x => x.Tenant == _tenant && x.UserKey == userKey, ct);

        if (rootProjection is null)
            return;

        var chainProjections = await DbSetChain
            .Where(x => x.Tenant == _tenant && x.UserKey == userKey)
            .ToListAsync(ct);

        foreach (var chainProjection in chainProjections)
        {
            var sessions = await DbSetSession
                .Where(x => x.Tenant == _tenant && x.ChainId == chainProjection.ChainId)
                .ToListAsync(ct);

            foreach (var sessionProjection in sessions)
            {
                if (sessionProjection.RevokedAt is not null)
                    continue;

                var sessionDomain = sessionProjection.ToDomain().Revoke(at);

                sessionDomain.UpdateProjection(sessionProjection);
                sessionProjection.Version++;
            }

            if (chainProjection.RevokedAt is null)
            {
                var chainDomain = chainProjection.ToDomain().Revoke(at);

                chainDomain.UpdateProjection(chainProjection);
                chainProjection.Version++;
            }
        }

        if (rootProjection.RevokedAt is null)
        {
            var rootDomain = rootProjection.ToDomain().Revoke(at);

            rootDomain.UpdateProjection(rootProjection);
            rootProjection.Version++;
        }
    }
}
