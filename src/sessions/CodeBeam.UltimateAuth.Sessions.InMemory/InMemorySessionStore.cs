using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Domain.Session;
using System.Security;

namespace CodeBeam.UltimateAuth.Sessions.InMemory;

public sealed class InMemorySessionStore<TUserId> : ISessionStore<TUserId>
{
    private readonly ISessionStoreFactory _factory;

    public InMemorySessionStore(ISessionStoreFactory factory)
    {
        _factory = factory;
    }

    private ISessionStoreKernel<TUserId> Kernel(string? tenantId)
        => _factory.Create<TUserId>(tenantId);

    public Task<ISession<TUserId>?> GetSessionAsync(
        string? tenantId,
        AuthSessionId sessionId)
        => Kernel(tenantId).GetSessionAsync(tenantId, sessionId);

    public async Task CreateSessionAsync(
        IssuedSession<TUserId> issued,
        SessionStoreContext<TUserId> ctx)
    {
        var k = Kernel(ctx.TenantId);

        await k.ExecuteAsync(async () =>
        {
            var now = ctx.IssuedAt;

            // Root
            var root =
                await k.GetSessionRootAsync(ctx.TenantId, ctx.UserId)
                ?? UAuthSessionRoot<TUserId>.Create(
                    ctx.TenantId,
                    ctx.UserId,
                    now);

            // Chain
            ISessionChain<TUserId> chain;

            if (ctx.ChainId is not null)
            {
                chain = await k.GetChainAsync(ctx.TenantId, ctx.ChainId.Value)
                    ?? throw new InvalidOperationException("Chain not found.");
            }
            else
            {
                chain = UAuthSessionChain<TUserId>.Create(
                    ChainId.New(),
                    ctx.TenantId,
                    ctx.UserId,
                    root.SecurityVersion,
                    ClaimsSnapshot.Empty);

                root = root.AttachChain(chain, now);
            }

            // Session
            var session = UAuthSession<TUserId>.Create(
                issued.Session.SessionId,
                ctx.TenantId,
                ctx.UserId,
                chain.ChainId,
                now,
                issued.Session.ExpiresAt,
                ctx.DeviceInfo,
                issued.Session.Claims,
                metadata: null);

            await k.SaveSessionRootAsync(ctx.TenantId, root);
            await k.SaveChainAsync(ctx.TenantId, chain);
            await k.SaveSessionAsync(ctx.TenantId, session);
            await k.SetActiveSessionIdAsync(
                ctx.TenantId,
                chain.ChainId,
                session.SessionId);
        });
    }

    public async Task RotateSessionAsync(
        AuthSessionId currentSessionId,
        IssuedSession<TUserId> issued,
        SessionStoreContext<TUserId> ctx)
    {
        var k = Kernel(ctx.TenantId);

        await k.ExecuteAsync(async () =>
        {
            var now = ctx.IssuedAt;

            var old = await k.GetSessionAsync(ctx.TenantId, currentSessionId)
                ?? throw new SecurityException("Session not found.");

            var chain = await k.GetChainAsync(ctx.TenantId, old.ChainId)
                ?? throw new SecurityException("Chain not found.");

            var newSession = UAuthSession<TUserId>.Create(
                issued.Session.SessionId,
                ctx.TenantId,
                ctx.UserId,
                chain.ChainId,
                now,
                issued.Session.ExpiresAt,
                ctx.DeviceInfo,
                issued.Session.Claims,
                metadata: null);

            await k.SaveSessionAsync(ctx.TenantId, newSession);
            await k.SetActiveSessionIdAsync(
                ctx.TenantId,
                chain.ChainId,
                newSession.SessionId);

            await k.RevokeSessionAsync(
                ctx.TenantId,
                currentSessionId,
                now);
        });
    }

    public Task RevokeSessionAsync(
        string? tenantId,
        AuthSessionId sessionId,
        DateTimeOffset at)
        => Kernel(tenantId).RevokeSessionAsync(tenantId, sessionId, at);

    public async Task RevokeAllSessionsAsync(
        string? tenantId,
        TUserId userId,
        DateTimeOffset at)
    {
        var k = Kernel(tenantId);

        await k.ExecuteAsync(async () =>
        {
            var root = await k.GetSessionRootAsync(tenantId, userId);
            if (root is null)
                return;

            foreach (var chain in root.Chains)
            {
                await k.RevokeChainAsync(tenantId, chain.ChainId, at);

                if (chain.ActiveSessionId is not null)
                {
                    await k.RevokeSessionAsync(
                        tenantId,
                        chain.ActiveSessionId.Value,
                        at);
                }
            }

            await k.RevokeSessionRootAsync(tenantId, userId, at);
        });
    }

    public async Task RevokeChainAsync(
        string? tenantId,
        ChainId chainId,
        DateTimeOffset at)
    {
        var k = Kernel(tenantId);

        await k.ExecuteAsync(async () =>
        {
            var chain = await k.GetChainAsync(tenantId, chainId);
            if (chain is null)
                return;

            await k.RevokeChainAsync(tenantId, chainId, at);

            if (chain.ActiveSessionId is not null)
            {
                await k.RevokeSessionAsync(
                    tenantId,
                    chain.ActiveSessionId.Value,
                    at);
            }
        });
    }
}
