using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class EfCoreSessionStoreKernel : ISessionStoreKernel
{
    private readonly UltimateAuthSessionDbContext _db;
    private readonly TenantContext _tenant;

    public EfCoreSessionStoreKernel(UltimateAuthSessionDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            var connection = _db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            await using var tx = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            _db.Database.UseTransaction(tx);

            try
            {
                await action(ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
            finally
            {
                _db.Database.UseTransaction(null);
            }
        });
    }

    public async Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId)
    {
        var projection = await _db.Sessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SessionId == sessionId);

        return projection?.ToDomain();
    }

    public async Task SaveSessionAsync(UAuthSession session)
    {
        var projection = session.ToProjection();

        var exists = await _db.Sessions
            .AnyAsync(x => x.SessionId == session.SessionId);

        if (exists)
            _db.Sessions.Update(projection);
        else
            _db.Sessions.Add(projection);
    }

    public async Task RevokeSessionAsync(AuthSessionId sessionId, DateTimeOffset at)
    {
        var projection = await _db.Sessions
            .SingleOrDefaultAsync(x => x.SessionId == sessionId);

        if (projection is null)
            return;

        var session = projection.ToDomain();
        if (session.IsRevoked)
            return;

        var revoked = session.Revoke(at);
        _db.Sessions.Update(revoked.ToProjection());
    }

    public async Task<UAuthSessionChain?> GetChainAsync(SessionChainId chainId)
    {
        var projection = await _db.Chains
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ChainId == chainId);

        return projection?.ToDomain();
    }

    public async Task SaveChainAsync(UAuthSessionChain chain)
    {
        var projection = chain.ToProjection();

        var exists = await _db.Chains
            .AnyAsync(x => x.ChainId == chain.ChainId);

        if (exists)
            _db.Chains.Update(projection);
        else
            _db.Chains.Add(projection);
    }

    public async Task RevokeChainAsync(SessionChainId chainId, DateTimeOffset at)
    {
        var projection = await _db.Chains
            .SingleOrDefaultAsync(x => x.ChainId == chainId);

        if (projection is null)
            return;

        var chain = projection.ToDomain();
        if (chain.IsRevoked)
            return;

        _db.Chains.Update(chain.Revoke(at).ToProjection());
    }

    public async Task<AuthSessionId?> GetActiveSessionIdAsync(SessionChainId chainId)
    {
        return await _db.Chains
            .AsNoTracking()
            .Where(x => x.ChainId == chainId)
            .Select(x => x.ActiveSessionId)
            .SingleOrDefaultAsync();
    }

    public async Task SetActiveSessionIdAsync(SessionChainId chainId, AuthSessionId sessionId)
    {
        var projection = await _db.Chains
            .SingleOrDefaultAsync(x => x.ChainId == chainId);

        if (projection is null)
            return;

        projection.ActiveSessionId = sessionId;
        _db.Chains.Update(projection);
    }

    public async Task<UAuthSessionRoot?> GetSessionRootByUserAsync(UserKey userKey)
    {
        var rootProjection = await _db.Roots
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserKey == userKey);

        if (rootProjection is null)
            return null;

        var chains = await _db.Chains
            .AsNoTracking()
            .Where(x => x.UserKey == userKey)
            .ToListAsync();

        return rootProjection.ToDomain(chains.Select(c => c.ToDomain()).ToList());
    }

    public async Task SaveSessionRootAsync(UAuthSessionRoot root)
    {
        var projection = root.ToProjection();

        var exists = await _db.Roots
            .AnyAsync(x => x.RootId == root.RootId);

        if (exists)
            _db.Roots.Update(projection);
        else
            _db.Roots.Add(projection);
    }

    public async Task RevokeSessionRootAsync(UserKey userKey, DateTimeOffset at)
    {
        var projection = await _db.Roots.SingleOrDefaultAsync(x => x.UserKey == userKey);

        if (projection is null)
            return;

        var root = projection.ToDomain();
        _db.Roots.Update(root.Revoke(at).ToProjection());
    }

    public async Task<SessionChainId?> GetChainIdBySessionAsync(AuthSessionId sessionId)
    {
        return await _db.Sessions
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .Select(x => (SessionChainId?)x.ChainId)
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey)
    {
        var projections = await _db.Chains
            .AsNoTracking()
            .Where(x => x.UserKey == userKey)
            .ToListAsync();

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId)
    {
        var projections = await _db.Sessions
            .AsNoTracking()
            .Where(x => x.ChainId == chainId)
            .ToListAsync();

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<UAuthSessionRoot?> GetSessionRootByIdAsync(SessionRootId rootId)
    {
        var rootProjection = await _db.Roots
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.RootId == rootId);

        if (rootProjection is null)
            return null;

        var chains = await _db.Chains
            .AsNoTracking()
            .Where(x => x.RootId == rootId)
            .ToListAsync();

        return rootProjection.ToDomain(chains.Select(c => c.ToDomain()).ToList());
    }


    public async Task DeleteExpiredSessionsAsync(DateTimeOffset at)
    {
        var projections = await _db.Sessions
            .Where(x => x.ExpiresAt <= at && !x.IsRevoked)
            .ToListAsync();

        foreach (var p in projections)
        {
            var revoked = p.ToDomain().Revoke(at);
            _db.Sessions.Update(revoked.ToProjection());
        }
    }

}
