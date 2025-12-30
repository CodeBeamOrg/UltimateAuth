using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.EntityFrameworkCore;
using System.Security;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class EfCoreSessionStore<TUserId> : ISessionStore<TUserId>
{
    private readonly EfCoreSessionStoreKernel<TUserId> _kernel;
    private readonly UltimateAuthSessionDbContext<TUserId> _db;

    public EfCoreSessionStore(EfCoreSessionStoreKernel<TUserId> kernel, UltimateAuthSessionDbContext<TUserId> db)
    {
        _kernel = kernel;
        _db = db;
    }

    public async Task<ISession<TUserId>?> GetSessionAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default)
    {
        var projection = await _db.Sessions
            .AsNoTracking()
            .Where(x =>
                x.SessionId == sessionId &&
                x.TenantId == tenantId)
            .SingleOrDefaultAsync(ct);

        if (projection is null)
            return null;

        return projection.ToDomain();
    }

    public async Task CreateSessionAsync(IssuedSession<TUserId> issued, SessionStoreContext<TUserId> ctx, CancellationToken ct = default)
    {
        await _kernel.ExecuteAsync(async ct =>
        {
            var now = ctx.IssuedAt;

            var rootProjection = await _db.Roots.SingleOrDefaultAsync(x => x.TenantId == ctx.TenantId && x.UserId!.Equals(ctx.UserId), ct);

            ISessionRoot<TUserId> root;

            if (rootProjection is null)
            {
                root = UAuthSessionRoot<TUserId>.Create(ctx.TenantId, ctx.UserId, now);
                _db.Roots.Add(root.ToProjection());
            }
            else
            {
                var chains = await LoadChainsAsync(ctx, ct);
                root = rootProjection.ToDomain(chains);
            }


            ISessionChain<TUserId> chain;

            if (ctx.ChainId is not null)
            {
                var chainProjection = await _db.Chains.SingleAsync(x => x.ChainId == ctx.ChainId.Value, ct);
                chain = chainProjection.ToDomain();
            }
            else
            {
                chain = UAuthSessionChain<TUserId>.Create(
                    ChainId.New(),
                    ctx.TenantId,
                    ctx.UserId,
                    root.SecurityVersion,
                    ClaimsSnapshot.Empty);

                _db.Chains.Add(chain.ToProjection());
                root = root.AttachChain(chain, now);
            }

            var session = UAuthSession<TUserId>.Create(
                issued.Session.SessionId,
                ctx.TenantId,
                ctx.UserId,
                chain.ChainId,
                now,
                issued.Session.ExpiresAt,
                ctx.DeviceInfo,
                issued.Session.Claims,
                metadata: SessionMetadata.Empty
            );

            _db.Sessions.Add(session.ToProjection());
            var updatedChain = chain.AttachSession(session.SessionId);
            _db.Chains.Update(updatedChain.ToProjection());

        }, ct);
    }

    public async Task RotateSessionAsync(AuthSessionId currentSessionId, IssuedSession<TUserId> issued, SessionStoreContext<TUserId> ctx, CancellationToken ct = default)
    {
        await _kernel.ExecuteAsync(async ct =>
        {
            var now = ctx.IssuedAt;

            var oldSessionProjection = await _db.Sessions.SingleOrDefaultAsync(
                x => x.SessionId == currentSessionId &&
                     x.TenantId == ctx.TenantId,
                ct);

            if (oldSessionProjection is null)
                throw new SecurityException("Session not found.");

            var oldSession = oldSessionProjection.ToDomain();

            var chainProjection = await _db.Chains.SingleOrDefaultAsync(
                x => x.ChainId == oldSession.ChainId, ct);

            if (chainProjection is null)
                throw new SecurityException("Chain not found.");

            var chain = chainProjection.ToDomain();

            if (chain.IsRevoked)
                throw new SecurityException("Session chain is revoked.");

            var newSession = UAuthSession<TUserId>.Create(
                issued.Session.SessionId,
                ctx.TenantId,
                ctx.UserId,
                chain.ChainId,
                now,
                issued.Session.ExpiresAt,
                ctx.DeviceInfo,
                issued.Session.Claims,
                metadata: SessionMetadata.Empty
            );

            _db.Sessions.Add(newSession.ToProjection());

            var updatedChain = chain.RotateSession(newSession.SessionId);
            _db.Chains.Update(updatedChain.ToProjection());

            var revokedOldSession = oldSession.Revoke(now);
            _db.Sessions.Update(revokedOldSession.ToProjection());

        }, ct);
    }

    public async Task RevokeAllSessionsAsync(string? tenantId, TUserId userId, DateTimeOffset at, CancellationToken ct = default)
    {
        await _kernel.ExecuteAsync(async ct =>
        {
            var rootProjection = await _db.Roots
                .SingleOrDefaultAsync(
                    x => x.TenantId == tenantId &&
                         x.UserId!.Equals(userId),
                    ct);

            if (rootProjection is null)
                return;

            var chainProjections = await _db.Chains
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.UserId!.Equals(userId))
                .ToListAsync(ct);

            foreach (var chainProjection in chainProjections)
            {
                var chain = chainProjection.ToDomain();

                if (chain.IsRevoked)
                    continue;

                var revokedChain = chain.Revoke(at);
                _db.Chains.Update(revokedChain.ToProjection());

                if (chain.ActiveSessionId is not null)
                {
                    var sessionProjection = await _db.Sessions
                        .SingleOrDefaultAsync(
                            x => x.SessionId == chain.ActiveSessionId &&
                                 x.TenantId == tenantId,
                            ct);

                    if (sessionProjection is not null)
                    {
                        var session = sessionProjection.ToDomain();
                        var revokedSession = session.Revoke(at);
                        _db.Sessions.Update(revokedSession.ToProjection());
                    }
                }
            }

            var root = rootProjection.ToDomain(chainProjections
                .Select(c => c.ToDomain())
                .ToList());

            var revokedRoot = root.Revoke(at);
            _db.Roots.Update(revokedRoot.ToProjection());

        }, ct);
    }

    public async Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        await _kernel.ExecuteAsync(async ct =>
        {
            var chainProjection = await _db.Chains
                .SingleOrDefaultAsync(
                    x => x.ChainId == chainId &&
                         x.TenantId == tenantId,
                    ct);

            if (chainProjection is null)
                return;

            var chain = chainProjection.ToDomain();

            if (chain.IsRevoked)
                return;

            var revokedChain = chain.Revoke(at);
            _db.Chains.Update(revokedChain.ToProjection());

            if (chain.ActiveSessionId is not null)
            {
                var sessionProjection = await _db.Sessions
                    .SingleOrDefaultAsync(
                        x => x.SessionId == chain.ActiveSessionId &&
                             x.TenantId == tenantId,
                        ct);

                if (sessionProjection is not null)
                {
                    var session = sessionProjection.ToDomain();
                    var revokedSession = session.Revoke(at);
                    _db.Sessions.Update(revokedSession.ToProjection());
                }
            }

        }, ct);
    }

    public async Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
    {
        await _kernel.ExecuteAsync(async ct =>
        {
            var sessionProjection = await _db.Sessions
                .SingleOrDefaultAsync(
                    x => x.SessionId == sessionId &&
                         x.TenantId == tenantId,
                    ct);

            if (sessionProjection is null)
                return;

            var session = sessionProjection.ToDomain();

            if (session.IsRevoked)
                return;

            var revokedSession = session.Revoke(at);
            _db.Sessions.Update(revokedSession.ToProjection());

        }, ct);
    }

    public async Task<IReadOnlyList<ISession<TUserId>>> GetSessionsByChainAsync(string? tenantId, ChainId chainId, CancellationToken ct = default)
    {
        var projections = await _db.Sessions
            .AsNoTracking()
            .Where(x =>
                x.ChainId == chainId &&
                x.TenantId == tenantId)
            .ToListAsync(ct);

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
    {
        var projections = await _db.Chains
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId!.Equals(userId))
            .ToListAsync(ct);

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task<ISessionChain<TUserId>?> GetChainAsync(string? tenantId, ChainId chainId, CancellationToken ct = default)
    {
        var projection = await _db.Chains
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ChainId == chainId &&
                     x.TenantId == tenantId,
                ct);

        return projection?.ToDomain();
    }

    public async Task<ChainId?> GetChainIdBySessionAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default)
    {
        return await _db.Sessions
            .AsNoTracking()
            .Where(x =>
                x.SessionId == sessionId &&
                x.TenantId == tenantId)
            .Select(x => (ChainId?)x.ChainId)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<AuthSessionId?> GetActiveSessionIdAsync(string? tenantId, ChainId chainId, CancellationToken ct = default)
    {
        return await _db.Chains
            .AsNoTracking()
            .Where(x =>
                x.ChainId == chainId &&
                x.TenantId == tenantId)
            .Select(x => x.ActiveSessionId)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<ISessionRoot<TUserId>?> GetSessionRootAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
    {
        var rootProjection = await _db.Roots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.UserId!.Equals(userId),
                ct);

        if (rootProjection is null)
            return null;

        var chainProjections = await _db.Chains
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId!.Equals(userId))
            .ToListAsync(ct);

        return rootProjection.ToDomain(chainProjections.Select(x => x.ToDomain()).ToList());
    }


    private async Task<IReadOnlyList<ISessionChain<TUserId>>> LoadChainsAsync(SessionStoreContext<TUserId> ctx, CancellationToken ct)
    {
        var chainProjections = await _db.Chains
            .AsNoTracking()
            .Where(x =>
                x.TenantId == ctx.TenantId &&
                x.UserId!.Equals(ctx.UserId))
            .ToListAsync(ct);

        return chainProjections
            .Select(x => x.ToDomain())
            .ToList();
    }
}
