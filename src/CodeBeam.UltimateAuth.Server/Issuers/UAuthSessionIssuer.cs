using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using System.Security;

namespace CodeBeam.UltimateAuth.Server.Issuers;

public sealed class UAuthSessionIssuer : ISessionIssuer
{
    private readonly ISessionStoreKernelFactory _kernelFactory;
    private readonly IOpaqueTokenGenerator _opaqueGenerator;
    private readonly UAuthServerOptions _options;

    public UAuthSessionIssuer(
        ISessionStoreKernelFactory kernelFactory,
        IOpaqueTokenGenerator opaqueGenerator,
        IOptions<UAuthServerOptions> options)
    {
        _kernelFactory = kernelFactory;
        _opaqueGenerator = opaqueGenerator;
        _options = options.Value;
    }

    public async Task<IssuedSession> IssueLoginSessionAsync(AuthenticatedSessionContext context, CancellationToken ct = default)
    {
        // Defensive guard — enforcement belongs to Authority
        if (_options.Mode == UAuthMode.PureJwt)
        {
            throw new InvalidOperationException("Session issuance is not allowed in PureJwt mode.");
        }

        var now = context.Now;
        var opaqueSessionId = _opaqueGenerator.Generate();
        if (!AuthSessionId.TryCreate(opaqueSessionId, out AuthSessionId sessionId))
            throw new InvalidCastException("Can't create opaque id.");

        var expiresAt = now.Add(_options.Session.Lifetime);

        if (_options.Session.MaxLifetime is not null)
        {
            var absoluteExpiry = now.Add(_options.Session.MaxLifetime.Value);
            if (absoluteExpiry < expiresAt)
                expiresAt = absoluteExpiry;
        }

        var session = UAuthSession.Create(
            sessionId: sessionId,
            tenant: context.Tenant,
            userKey: context.UserKey,
            chainId: SessionChainId.Unassigned,
            now: now,
            expiresAt: expiresAt,
            claims: context.Claims,
            device: context.Device,
            metadata: context.Metadata
        );

        var issued = new IssuedSession
        {
            Session = session,
            OpaqueSessionId = opaqueSessionId,
            IsMetadataOnly = _options.Mode == UAuthMode.SemiHybrid
        };

        var kernel = _kernelFactory.Create(context.Tenant);

        await kernel.ExecuteAsync(async _ =>
        {
            var root = await kernel.GetSessionRootByUserAsync(context.UserKey)
                ?? UAuthSessionRoot.Create(context.Tenant, context.UserKey, now);

            UAuthSessionChain chain;

            if (context.ChainId is not null)
            {
                chain = await kernel.GetChainAsync(context.ChainId.Value)
                    ?? throw new SecurityException("Chain not found.");
            }
            else
            {
                chain = UAuthSessionChain.Create(
                    SessionChainId.New(),
                    root.RootId,
                    context.Tenant,
                    context.UserKey,
                    root.SecurityVersion,
                    ClaimsSnapshot.Empty);

                await kernel.SaveChainAsync(chain);
                root = root.AttachChain(chain, now);
            }

            var boundSession = session.WithChain(chain.ChainId);

            await kernel.SaveSessionAsync(boundSession);
            await kernel.SetActiveSessionIdAsync(chain.ChainId, boundSession.SessionId);
            await kernel.SaveSessionRootAsync(root);
        }, ct);

        return issued;
    }

    public async Task<IssuedSession> RotateSessionAsync(SessionRotationContext context, CancellationToken ct = default)
    {
        var kernel = _kernelFactory.Create(context.Tenant);
        var now = context.Now;

        var opaqueSessionId = _opaqueGenerator.Generate();
        if (!AuthSessionId.TryCreate(opaqueSessionId, out var newSessionId))
            throw new InvalidCastException("Can't create opaque session id.");

        var expiresAt = now.Add(_options.Session.Lifetime);
        if (_options.Session.MaxLifetime is not null)
        {
            var absoluteExpiry = now.Add(_options.Session.MaxLifetime.Value);
            if (absoluteExpiry < expiresAt)
                expiresAt = absoluteExpiry;
        }

        var issued = new IssuedSession
        {
            Session = UAuthSession.Create(
                sessionId: newSessionId,
                tenant: context.Tenant,
                userKey: context.UserKey,
                chainId: SessionChainId.Unassigned,
                now: now,
                expiresAt: expiresAt,
                device: context.Device,
                claims: context.Claims,
                metadata: context.Metadata
            ),
            OpaqueSessionId = opaqueSessionId,
            IsMetadataOnly = _options.Mode == UAuthMode.SemiHybrid
        };

        await kernel.ExecuteAsync(async _ =>
        {
            var oldSession = await kernel.GetSessionAsync(context.CurrentSessionId)
                ?? throw new SecurityException("Session not found");

            if (oldSession.IsRevoked || oldSession.ExpiresAt <= now)
                throw new SecurityException("Session is not valid");

            var chain = await kernel.GetChainAsync(oldSession.ChainId)
                ?? throw new SecurityException("Chain not found");

            var bound = issued.Session.WithChain(chain.ChainId);

            await kernel.SaveSessionAsync(bound);
            await kernel.SetActiveSessionIdAsync(chain.ChainId, bound.SessionId);
            await kernel.RevokeSessionAsync(oldSession.SessionId, now);
        }, ct);

        return issued;
    }

    public async Task RevokeSessionAsync(TenantKey tenant, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _kernelFactory.Create(tenant);
        await kernel.ExecuteAsync(_ => kernel.RevokeSessionAsync(sessionId, at), ct);
    }

    public async Task RevokeChainAsync(TenantKey tenant, SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _kernelFactory.Create(tenant);
        await kernel.ExecuteAsync(_ => kernel.RevokeChainAsync(chainId, at), ct);
    }

    public async Task RevokeAllChainsAsync(TenantKey tenant, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _kernelFactory.Create(tenant);
        await kernel.ExecuteAsync(async _ =>
        {
            var chains = await kernel.GetChainsByUserAsync(userKey);

            foreach (var chain in chains)
            {
                if (exceptChainId.HasValue && chain.ChainId == exceptChainId.Value)
                    continue;

                if (!chain.IsRevoked)
                    await kernel.RevokeChainAsync(chain.ChainId, at);

                var activeSessionId = await kernel.GetActiveSessionIdAsync(chain.ChainId);
                if (activeSessionId is not null)
                    await kernel.RevokeSessionAsync(activeSessionId.Value, at);
            }
        }, ct);
    }

    // TODO: Discuss revoking chains/sessions when root is revoked
    public async Task RevokeRootAsync(TenantKey tenant, UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _kernelFactory.Create(tenant);
        await kernel.ExecuteAsync(_ => kernel.RevokeSessionRootAsync(userKey, at), ct);
    }

}
