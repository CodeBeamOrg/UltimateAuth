using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using System.Security;

public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ISessionStoreKernelFactory _factory;
    private readonly UAuthServerOptions _options;

    public InMemorySessionStore(ISessionStoreKernelFactory factory, IOptions<UAuthServerOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    private ISessionStoreKernel Kernel(string? tenantId)
        => _factory.Create(tenantId);

    public Task<ISession?> GetSessionAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default)
        => Kernel(tenantId).GetSessionAsync(sessionId);

    public async Task CreateSessionAsync(IssuedSession issued, SessionStoreContext ctx, CancellationToken ct = default)
    {
        var k = Kernel(ctx.TenantId);

        await k.ExecuteAsync(async (ct) =>
        {
            var now = ctx.IssuedAt;

            var root = await k.GetSessionRootByUserAsync(ctx.UserKey) ?? UAuthSessionRoot.Create(ctx.TenantId, ctx.UserKey, now);
            ISessionChain chain;

            if (ctx.ChainId is not null)
            {
                chain = await k.GetChainAsync(ctx.ChainId.Value) ?? throw new InvalidOperationException("Chain not found.");
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

                root = root.AttachChain(chain, now);
            }

            var session = issued.Session;

            if (!session.ChainId.IsUnassigned)
            {
                throw new InvalidOperationException("Issued session already has a chain assigned.");
            }

            session = session.WithChain(chain.ChainId);

            // Persist (order intentional)
            await k.SaveSessionRootAsync(root);
            await k.SaveChainAsync(chain);
            await k.SaveSessionAsync(session);
            await k.SetActiveSessionIdAsync(chain.ChainId, session.SessionId);
        }, ct);
    }

    public async Task RotateSessionAsync(AuthSessionId currentSessionId, IssuedSession issued, SessionStoreContext ctx, CancellationToken ct = default)
    {
        var k = Kernel(ctx.TenantId);

        await k.ExecuteAsync(async (ct) =>
        {
            var now = ctx.IssuedAt;

            var old = await k.GetSessionAsync(currentSessionId)
                ?? throw new SecurityException("Session not found.");

            if (old.IsRevoked || old.ExpiresAt <= now)
                throw new SecurityException("Session is no longer valid.");

            var chain = await k.GetChainAsync(old.ChainId)
                ?? throw new SecurityException("Chain not found.");

            if (chain.IsRevoked)
                throw new SecurityException("Chain is revoked.");

            var newSession = ((UAuthSession)issued.Session).WithChain(chain.ChainId);

            await k.SaveSessionAsync(newSession);
            await k.SetActiveSessionIdAsync(chain.ChainId, newSession.SessionId);
            await k.RevokeSessionAsync(old.SessionId, now);
        }, ct);
    }

    public async Task<bool> TouchSessionAsync(AuthSessionId sessionId, DateTimeOffset at, SessionTouchMode mode = SessionTouchMode.IfNeeded, CancellationToken ct = default)
    {
        var k = Kernel(null);
        bool touched = false;

        await k.ExecuteAsync(async (ct) =>
        {
            var session = await k.GetSessionAsync(sessionId);
            if (session is null || session.IsRevoked)
                return;

            if (mode == SessionTouchMode.IfNeeded)
            {
                var elapsed = at - session.LastSeenAt;
                if (elapsed < _options.Session.TouchInterval)
                    return;
            }

            var updated = session.Touch(at);
            await k.SaveSessionAsync(updated);

            touched = true;
        }, ct);

        return touched;
    }

    public Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
        => Kernel(tenantId).RevokeSessionAsync(sessionId, at);

    public async Task RevokeAllChainsAsync(string? tenantId, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default)
    {
        var k = Kernel(tenantId);

        await k.ExecuteAsync(async (ct) =>
        {
            var chains = await k.GetChainsByUserAsync(userKey);

            foreach (var chain in chains)
            {
                if (exceptChainId.HasValue && chain.ChainId == exceptChainId.Value)
                    continue;

                await k.RevokeChainAsync(chain.ChainId, at);

                if (chain.ActiveSessionId is not null)
                    await k.RevokeSessionAsync(chain.ActiveSessionId.Value, at);
            }
        }, ct);
    }

    public Task RevokeChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
        => Kernel(tenantId).RevokeChainAsync(chainId, at);

    public Task RevokeRootAsync(string? tenantId, UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
        => Kernel(tenantId).RevokeSessionRootAsync(userKey, at);
}
