using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using System.Security;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthSessionIssuer : ISessionIssuer
{
    private readonly ISessionStoreFactory _kernelFactory;
    private readonly IOpaqueTokenGenerator _opaqueGenerator;
    private readonly UAuthServerOptions _options;

    public UAuthSessionIssuer(ISessionStoreFactory kernelFactory, IOpaqueTokenGenerator opaqueGenerator, IOptions<UAuthServerOptions> options)
    {
        _kernelFactory = kernelFactory;
        _opaqueGenerator = opaqueGenerator;
        _options = options.Value;
    }

    public async Task<IssuedSession> IssueSessionAsync(AuthenticatedSessionContext context, CancellationToken ct = default)
    {
        // Defensive guard — enforcement belongs to Authority
        if (context.Mode == UAuthMode.PureJwt)
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

        var kernel = _kernelFactory.Create(context.Tenant);

        IssuedSession? issued = null;

        await kernel.ExecuteAsync(async _ =>
        {
            var root = await kernel.GetRootByUserAsync(context.UserKey);

            bool isNewRoot = root is null;
            long rootExpectedVersion = 0;

            if (isNewRoot)
            {
                root = UAuthSessionRoot.Create(context.Tenant, context.UserKey, now);
                await kernel.CreateRootAsync(root);
            }
            else
            {
                if (root!.IsRevoked)
                    throw new SecurityException("Session root is revoked.");

                rootExpectedVersion = root.Version;
            }

            UAuthSessionChain chain;
            bool isNewChain = false;

            if (context.ChainId is not null)
            {
                if (isNewRoot)
                    throw new SecurityException("ChainId provided but session root does not exist.");

                chain = await kernel.GetChainAsync(context.ChainId.Value) ?? throw new SecurityException("Chain not found.");

                if (chain.IsRevoked)
                    throw new SecurityException("Chain is revoked.");

                if (chain.Tenant != context.Tenant || chain.UserKey != context.UserKey)
                    throw new SecurityException("Chain does not belong to the current user/tenant.");
            }
            else
            {
                isNewChain = true;

                chain = UAuthSessionChain.Create(
                    SessionChainId.New(),
                    root.RootId,
                    context.Tenant,
                    context.UserKey,
                    root.SecurityVersion,
                    ClaimsSnapshot.Empty);

                await kernel.CreateChainAsync(chain);

                var updatedRoot = root.AttachChain(chain, now);

                if (isNewRoot)
                {
                    await kernel.SaveRootAsync(updatedRoot, 0);
                }
                else
                {
                    await kernel.SaveRootAsync(updatedRoot, rootExpectedVersion);
                }

                root = updatedRoot;
            }

            var session = UAuthSession.Create(
                sessionId: sessionId,
                tenant: context.Tenant,
                userKey: context.UserKey,
                chainId: SessionChainId.Unassigned,
                now: now,
                expiresAt: expiresAt,
                securityVersion: root.SecurityVersion,
                device: context.Device,
                claims: context.Claims,
                metadata: context.Metadata
            );

            await kernel.CreateSessionAsync(session);
            var bound = session.WithChain(chain.ChainId);
            await kernel.SaveSessionAsync(bound, 0);
            var updatedChain = chain.AttachSession(bound.SessionId);
            await kernel.SaveChainAsync(updatedChain, chain.Version);

            issued = new IssuedSession
            {
                Session = session,
                OpaqueSessionId = opaqueSessionId,
                IsMetadataOnly = context.Mode == UAuthMode.SemiHybrid
            };
        }, ct);

        if (issued == null)
            throw new InvalidCastException("Can't issued session.");
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

        IssuedSession? issued = null;

        await kernel.ExecuteAsync(async _ =>
        {
            var root = await kernel.GetRootByUserAsync(context.UserKey);
            if (root == null)
                throw new SecurityException("Session root not found");

            if (root.IsRevoked)
                throw new SecurityException("Session root is revoked");

            var oldSession = await kernel.GetSessionAsync(context.CurrentSessionId)
                ?? throw new SecurityException("Session not found");

            if (oldSession.IsRevoked || oldSession.ExpiresAt <= now)
                throw new SecurityException("Session is not valid");

            if (oldSession.SecurityVersionAtCreation != root.SecurityVersion)
                throw new SecurityException("Security version mismatch");

            var chain = await kernel.GetChainAsync(oldSession.ChainId)
                ?? throw new SecurityException("Chain not found");

            if (chain.IsRevoked)
                throw new SecurityException("Chain is revoked");

            if (chain.Tenant != context.Tenant || chain.UserKey != context.UserKey)
                throw new SecurityException("Chain does not belong to the current user/tenant.");

            var newSessionUnbound = UAuthSession.Create(
                sessionId: newSessionId,
                tenant: context.Tenant,
                userKey: context.UserKey,
                chainId: SessionChainId.Unassigned,
                now: now,
                expiresAt: expiresAt,
                securityVersion: root.SecurityVersion,
                device: context.Device,
                claims: context.Claims,
                metadata: context.Metadata
            );

            issued = new IssuedSession
            {
                Session = newSessionUnbound,
                OpaqueSessionId = opaqueSessionId,
                IsMetadataOnly = context.Mode == UAuthMode.SemiHybrid
            };

            var newSession = issued.Session.WithChain(chain.ChainId);

            await kernel.CreateSessionAsync(newSession);
            var chainExpected = chain.Version;
            var updatedChain = chain.RotateSession(newSession.SessionId);
            await kernel.SaveChainAsync(updatedChain, chainExpected);

            //await kernel.SetActiveSessionIdAsync(chain.ChainId, newSession.SessionId);

            var expected = oldSession.Version;
            var revokedOld = oldSession.Revoke(now);
            await kernel.SaveSessionAsync(revokedOld, expected);
        }, ct);

        if (issued == null)
            throw new InvalidCastException("Can't issued session.");
        return issued;
    }

    public async Task<bool> RevokeSessionAsync(TenantKey tenant, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _kernelFactory.Create(tenant);
        return await kernel.ExecuteAsync(_ => kernel.RevokeSessionAsync(sessionId, at), ct);
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
                {
                    var expectedChainVersion = chain.Version;
                    var revokedChain = chain.Revoke(at);
                    await kernel.SaveChainAsync(revokedChain, expectedChainVersion);
                }

                if (chain.ActiveSessionId is not null)
                {
                    var session = await kernel.GetSessionAsync(chain.ActiveSessionId.Value);
                    if (session is not null && !session.IsRevoked)
                    {
                        var expectedSessionVersion = session.Version;
                        var revokedSession = session.Revoke(at);
                        await kernel.SaveSessionAsync(revokedSession, expectedSessionVersion);
                    }
                }
            }
        }, ct);
    }

    // TODO: Discuss revoking chains/sessions when root is revoked
    public async Task RevokeRootAsync(TenantKey tenant, UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _kernelFactory.Create(tenant);
        await kernel.ExecuteAsync(_ => kernel.RevokeRootAsync(userKey, at), ct);
    }
}
