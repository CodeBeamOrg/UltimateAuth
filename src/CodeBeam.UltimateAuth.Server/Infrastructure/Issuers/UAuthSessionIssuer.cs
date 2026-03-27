using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using System.Security;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthSessionIssuer : ISessionIssuer
{
    private readonly ISessionStoreFactory _storeFactory;
    private readonly IOpaqueTokenGenerator _opaqueGenerator;
    private readonly UAuthServerOptions _options;

    public UAuthSessionIssuer(ISessionStoreFactory storeFactory, IOpaqueTokenGenerator opaqueGenerator, IOptions<UAuthServerOptions> options)
    {
        _storeFactory = storeFactory;
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

        var kernel = _storeFactory.Create(context.Tenant);

        IssuedSession? issued = null;

        await kernel.ExecuteAsync(async _ =>
        {
            var root = await kernel.GetRootByUserAsync(context.UserKey);

            if (root is null)
            {
                root = UAuthSessionRoot.Create(context.Tenant, context.UserKey, now);
                await kernel.CreateRootAsync(root);
            }
            else if (root.IsRevoked)
            {
                throw new UAuthValidationException("Session root revoked.");
            }

            UAuthSessionChain chain;

            if (context.ChainId is not null)
            {
                var existing = await kernel.GetChainAsync(context.ChainId.Value);

                if (existing is null)
                {
                    chain = UAuthSessionChain.Create(
                        SessionChainId.New(),
                        root.RootId,
                        context.Tenant,
                        context.UserKey,
                        now,
                        expiresAt,
                        context.Device,
                        ClaimsSnapshot.Empty,
                        root.SecurityVersion
                    );
                    await kernel.CreateChainAsync(chain);
                }
                else
                {
                    chain = existing;
                }

                //chain = await kernel.GetChainAsync(context.ChainId.Value)
                //    ?? throw new UAuthNotFoundException("Chain not found.");

                if (chain.IsRevoked)
                    throw new UAuthValidationException("Chain revoked.");

                if (chain.UserKey != context.UserKey || chain.Tenant != context.Tenant)
                    throw new UAuthValidationException("Invalid chain ownership.");
            }
            else
            {
                chain = UAuthSessionChain.Create(
                    SessionChainId.New(),
                    root.RootId,
                    context.Tenant,
                    context.UserKey,
                    now,
                    expiresAt,
                    context.Device,
                    ClaimsSnapshot.Empty,
                    root.SecurityVersion
                );

                await kernel.CreateChainAsync(chain);
            }

            var sessions = await kernel.GetSessionsByChainAsync(chain.ChainId);

            if (sessions.Count >= _options.Session.MaxSessionsPerChain)
            {
                var toDelete = sessions
                    .Where(s => s.SessionId != chain.ActiveSessionId)
                    .OrderBy(x => x.CreatedAt)
                    .Take(sessions.Count - _options.Session.MaxSessionsPerChain + 1)
                    .ToList();

                foreach (var old in toDelete)
                {
                    await kernel.RemoveSessionAsync(old.SessionId);
                }
            }

            var session = UAuthSession.Create(
                sessionId: sessionId,
                tenant: context.Tenant,
                userKey: context.UserKey,
                chainId: chain.ChainId,
                now: now,
                expiresAt: expiresAt,
                securityVersion: root.SecurityVersion,
                device: context.Device,
                claims: context.Claims,
                metadata: context.Metadata
            );

            await kernel.CreateSessionAsync(session);

            var updatedChain = chain.AttachSession(session.SessionId, now);
            await kernel.SaveChainAsync(updatedChain, chain.Version);

            issued = new IssuedSession
            {
                Session = session,
                OpaqueSessionId = opaqueSessionId,
                IsMetadataOnly = context.Mode == UAuthMode.SemiHybrid
            };
        }, ct);

        if (issued == null)
            throw new InvalidCastException("Issue failed.");
        return issued;
    }

    public async Task<IssuedSession> RotateSessionAsync(SessionRotationContext context, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(context.Tenant);
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
            var updatedChain = chain.RotateSession(newSession.SessionId, now, context.Claims);
            await kernel.SaveChainAsync(updatedChain, chainExpected);

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
        var kernel = _storeFactory.Create(tenant);
        return await kernel.ExecuteAsync(_ => kernel.RevokeSessionAsync(sessionId, at), ct);
    }

    public async Task RevokeChainAsync(TenantKey tenant, SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(tenant);
        await kernel.ExecuteAsync(async _ =>
        {
            var chain = await kernel.GetChainAsync(chainId);
            if (chain is null)
                return;

            await kernel.RevokeChainCascadeAsync(chainId, at);
        }, ct);
    }

    public async Task RevokeAllChainsAsync(TenantKey tenant, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(tenant);
        await kernel.ExecuteAsync(async _ =>
        {
            var chains = await kernel.GetChainsByUserAsync(userKey);

            foreach (var chain in chains)
            {
                if (exceptChainId.HasValue && chain.ChainId == exceptChainId.Value)
                    continue;

                await kernel.RevokeChainCascadeAsync(chain.ChainId, at);
            }
        }, ct);
    }

    public async Task RevokeRootAsync(TenantKey tenant, UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(tenant);
        await kernel.ExecuteAsync(async _ =>
        {
            var root = await kernel.GetRootByUserAsync(userKey);
            if (root is null)
                return;

            await kernel.RevokeRootCascadeAsync(userKey, at);
        }, ct);
    }
}
