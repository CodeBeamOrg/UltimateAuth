using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.EntityFrameworkCore;
using System.Security;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class EfCoreSessionStore : ISessionStore
{
    private readonly EfCoreSessionStoreKernel _kernel;
    private readonly UltimateAuthSessionDbContext _db;

    public EfCoreSessionStore(EfCoreSessionStoreKernel kernel, UltimateAuthSessionDbContext db)
    {
        _kernel = kernel;
        _db = db;
    }

    public async Task<ISession?> GetSessionAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default)
    {
        var projection = await _db.Sessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.SessionId == sessionId &&
                     x.TenantId == tenantId,
                ct);

        return projection?.ToDomain();
    }

    public async Task CreateSessionAsync(IssuedSession issued, SessionStoreContext ctx, CancellationToken ct = default)
    {
        await _kernel.ExecuteAsync(async ct =>
        {
            var now = ctx.IssuedAt;

            var rootProjection = await _db.Roots
                .SingleOrDefaultAsync(
                    x => x.TenantId == ctx.TenantId &&
                         x.UserKey == ctx.UserKey,
                    ct);

            ISessionRoot root;

            if (rootProjection is null)
            {
                root = UAuthSessionRoot.Create(ctx.TenantId, ctx.UserKey, now);
                _db.Roots.Add(root.ToProjection());
            }
            else
            {
                var chainProjections = await _db.Chains
                    .AsNoTracking()
                    .Where(x => x.RootId == rootProjection.RootId)
                    .ToListAsync(ct);

                root = rootProjection.ToDomain(
                    chainProjections.Select(c => c.ToDomain()).ToList());
            }

            ISessionChain chain;

            if (ctx.ChainId is not null)
            {
                var chainProjection = await _db.Chains
                    .SingleAsync(x => x.ChainId == ctx.ChainId.Value, ct);

                chain = chainProjection.ToDomain();
            }
            else
            {
                chain = UAuthSessionChain.Create(
                    SessionChainId.New(),
                    root.RootId,
                    ctx.TenantId,
                    ctx.UserKey,
                    root.SecurityVersion,
                    ClaimsSnapshot.Empty);

                _db.Chains.Add(chain.ToProjection());
                root = root.AttachChain(chain, now);
            }

            var issuedSession = (UAuthSession)issued.Session;

            if (!issuedSession.ChainId.IsUnassigned)
                throw new InvalidOperationException("Issued session already has chain.");

            var session = issuedSession.WithChain(chain.ChainId);

            _db.Sessions.Add(session.ToProjection());

            var updatedChain = chain.AttachSession(session.SessionId);
            _db.Chains.Update(updatedChain.ToProjection());
        }, ct);
    }

    public async Task RotateSessionAsync(AuthSessionId currentSessionId, IssuedSession issued, SessionStoreContext ctx, CancellationToken ct = default)
    {
        await _kernel.ExecuteAsync(async ct =>
        {
            var now = ctx.IssuedAt;

            var oldProjection = await _db.Sessions
                .SingleOrDefaultAsync(
                    x => x.SessionId == currentSessionId &&
                         x.TenantId == ctx.TenantId,
                    ct);

            if (oldProjection is null)
                throw new SecurityException("Session not found.");

            var oldSession = oldProjection.ToDomain();

            if (oldSession.IsRevoked || oldSession.ExpiresAt <= now)
                throw new SecurityException("Session is no longer valid.");

            var chainProjection = await _db.Chains
                .SingleOrDefaultAsync(
                    x => x.ChainId == oldSession.ChainId,
                    ct);

            if (chainProjection is null)
                throw new SecurityException("Chain not found.");

            var chain = chainProjection.ToDomain();

            if (chain.IsRevoked)
                throw new SecurityException("Chain is revoked.");

            var newSession = ((UAuthSession)issued.Session)
                .WithChain(chain.ChainId);

            _db.Sessions.Add(newSession.ToProjection());

            var rotatedChain = chain.RotateSession(newSession.SessionId);
            _db.Chains.Update(rotatedChain.ToProjection());

            var revokedOld = oldSession.Revoke(now);
            _db.Sessions.Update(revokedOld.ToProjection());
        }, ct);
    }

    public async Task<bool> TouchSessionAsync(AuthSessionId sessionId, DateTimeOffset at, SessionTouchMode mode = SessionTouchMode.IfNeeded, CancellationToken ct = default)
    {
        var touched = false;

        await _kernel.ExecuteAsync(async ct =>
        {
            var projection = await _db.Sessions.SingleOrDefaultAsync(x => x.SessionId == sessionId, ct);

            if (projection is null)
                return;

            var session = projection.ToDomain();

            if (session.IsRevoked)
                return;

            if (mode == SessionTouchMode.IfNeeded && at - session.LastSeenAt < TimeSpan.FromMinutes(1))
                return;

            var updated = session.Touch(at);
            _db.Sessions.Update(updated.ToProjection());

            touched = true;
        }, ct);

        return touched;
    }

    public Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
        => _kernel.ExecuteAsync(async ct =>
        {
            var projection = await _db.Sessions.SingleOrDefaultAsync(x => x.SessionId == sessionId && x.TenantId == tenantId, ct);

            if (projection is null)
                return;

            var session = projection.ToDomain();

            if (session.IsRevoked)
                return;

            _db.Sessions.Update(session.Revoke(at).ToProjection());
        }, ct);

    public async Task RevokeAllChainsAsync(string? tenantId, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default)
    {
        await _kernel.ExecuteAsync(async ct =>
        {
            var chains = await _db.Chains
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.UserKey == userKey)
                .ToListAsync(ct);

            foreach (var chainProjection in chains)
            {
                if (exceptChainId.HasValue &&
                    chainProjection.ChainId == exceptChainId.Value)
                    continue;

                var chain = chainProjection.ToDomain();

                if (!chain.IsRevoked)
                    _db.Chains.Update(chain.Revoke(at).ToProjection());

                if (chain.ActiveSessionId is not null)
                {
                    var sessionProjection = await _db.Sessions.SingleOrDefaultAsync(x => x.SessionId == chain.ActiveSessionId, ct);

                    if (sessionProjection is not null)
                    {
                        var session = sessionProjection.ToDomain();
                        if (!session.IsRevoked)
                            _db.Sessions.Update(session.Revoke(at).ToProjection());
                    }
                }
            }
        }, ct);
    }

    public Task RevokeChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
        => _kernel.ExecuteAsync(async ct =>
        {
            var projection = await _db.Chains
                .SingleOrDefaultAsync(
                    x => x.ChainId == chainId &&
                         x.TenantId == tenantId,
                    ct);

            if (projection is null)
                return;

            var chain = projection.ToDomain();

            if (chain.IsRevoked)
                return;

            _db.Chains.Update(chain.Revoke(at).ToProjection());

            if (chain.ActiveSessionId is not null)
            {
                var sessionProjection = await _db.Sessions
                    .SingleOrDefaultAsync(x => x.SessionId == chain.ActiveSessionId, ct);

                if (sessionProjection is not null)
                {
                    var session = sessionProjection.ToDomain();
                    if (!session.IsRevoked)
                        _db.Sessions.Update(session.Revoke(at).ToProjection());
                }
            }
        }, ct);

    public Task RevokeRootAsync(string? tenantId, UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
        => _kernel.ExecuteAsync(async ct =>
        {
            var rootProjection = await _db.Roots
                .SingleOrDefaultAsync(
                    x => x.TenantId == tenantId &&
                         x.UserKey == userKey,
                    ct);

            if (rootProjection is null)
                return;

            var chainProjections = await _db.Chains
                .Where(x => x.RootId == rootProjection.RootId)
                .ToListAsync(ct);

            foreach (var chainProjection in chainProjections)
            {
                var chain = chainProjection.ToDomain();
                _db.Chains.Update(chain.Revoke(at).ToProjection());

                if (chain.ActiveSessionId is not null)
                {
                    var sessionProjection = await _db.Sessions
                        .SingleOrDefaultAsync(x => x.SessionId == chain.ActiveSessionId, ct);

                    if (sessionProjection is not null)
                    {
                        var session = sessionProjection.ToDomain();
                        _db.Sessions.Update(session.Revoke(at).ToProjection());
                    }
                }
            }

            var root = rootProjection.ToDomain(chainProjections.Select(c => c.ToDomain()).ToList());

            _db.Roots.Update(root.Revoke(at).ToProjection());
        }, ct);

    public async Task<IReadOnlyList<ISession>> GetSessionsByChainAsync(string? tenantId, SessionChainId chainId, CancellationToken ct = default)
    {
        var projections = await _db.Sessions
            .AsNoTracking()
            .Where(x =>
                x.ChainId == chainId &&
                x.TenantId == tenantId)
            .ToListAsync(ct);

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<ISessionChain>> GetChainsByUserAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
    {
        var projections = await _db.Chains
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserKey.Equals(userKey))
            .ToListAsync(ct);

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<ISessionChain?> GetChainAsync(string? tenantId, SessionChainId chainId, CancellationToken ct = default)
    {
        var projection = await _db.Chains
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ChainId == chainId &&
                     x.TenantId == tenantId,
                ct);

        return projection?.ToDomain();
    }

    public async Task<SessionChainId?> GetChainIdBySessionAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default)
    {
        return await _db.Sessions
            .AsNoTracking()
            .Where(x =>
                x.SessionId == sessionId &&
                x.TenantId == tenantId)
            .Select(x => (SessionChainId?)x.ChainId)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<AuthSessionId?> GetActiveSessionIdAsync(string? tenantId, SessionChainId chainId, CancellationToken ct = default)
    {
        return await _db.Chains
            .AsNoTracking()
            .Where(x =>
                x.ChainId == chainId &&
                x.TenantId == tenantId)
            .Select(x => x.ActiveSessionId)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<ISessionRoot?> GetSessionRootAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
    {
        var rootProjection = await _db.Roots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.UserKey!.Equals(userKey),
                ct);

        if (rootProjection is null)
            return null;

        var chainProjections = await _db.Chains
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserKey!.Equals(userKey))
            .ToListAsync(ct);

        return rootProjection.ToDomain(chainProjections.Select(x => x.ToDomain()).ToList());
    }
}
