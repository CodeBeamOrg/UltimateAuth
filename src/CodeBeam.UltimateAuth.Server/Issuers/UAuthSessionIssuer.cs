using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Domain.Session;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Security;

namespace CodeBeam.UltimateAuth.Server.Issuers
{
    public sealed class UAuthSessionIssuer<TUserId> : IHttpSessionIssuer<TUserId>
    {
        private readonly IOpaqueTokenGenerator _opaqueGenerator;
        private readonly ISessionStoreFactory _storeFactory;
        private readonly UAuthServerOptions _options;
        private readonly IUAuthSessionCookieManager _cookieManager;

        public UAuthSessionIssuer(IOpaqueTokenGenerator opaqueGenerator, ISessionStoreFactory storeFactory, IOptions<UAuthServerOptions> options, IUAuthSessionCookieManager cookieManager)
        {
            _opaqueGenerator = opaqueGenerator;
            _storeFactory = storeFactory;
            _options = options.Value;
            _cookieManager = cookieManager;
        }

        public Task<IssuedSession<TUserId>> IssueLoginSessionAsync(AuthenticatedSessionContext<TUserId> context, CancellationToken ct = default)
        {
            return IssueLoginInternalAsync(httpContext: null, context, ct);
        }

        public Task<IssuedSession<TUserId>> IssueLoginSessionAsync(HttpContext httpContext, AuthenticatedSessionContext<TUserId> context, CancellationToken ct = default)
        {
            if (httpContext is null)
                throw new ArgumentNullException(nameof(httpContext));

            return IssueLoginInternalAsync(httpContext, context, ct);
        }

        private async Task<IssuedSession<TUserId>> IssueLoginInternalAsync(HttpContext? httpContext, AuthenticatedSessionContext<TUserId> context, CancellationToken cancellationToken = default)
        {
            // Defensive guard — enforcement belongs to Authority
            if (_options.Mode == UAuthMode.PureJwt)
            {
                throw new InvalidOperationException("Session issuance is not allowed in PureJwt mode.");
            }

            var now = context.Now;
            var opaqueSessionId = _opaqueGenerator.Generate();

            var expiresAt = now.Add(_options.Session.Lifetime);

            if (_options.Session.MaxLifetime is not null)
            {
                var absoluteExpiry = now.Add(_options.Session.MaxLifetime.Value);
                if (absoluteExpiry < expiresAt)
                    expiresAt = absoluteExpiry;
            }

            var store = _storeFactory.Create<TUserId>(context.TenantId);

            IssuedSession<TUserId>? issued = null;

            await store.ExecuteAsync(async () =>
            {
                // Root
                var root =
                    await store.GetSessionRootAsync(context.TenantId, context.UserId)
                    ?? UAuthSessionRoot<TUserId>.Create(
                        context.TenantId,
                        context.UserId,
                        now);

                // Chain
                var claimsSnapshot = context.Claims;

                var chain = UAuthSessionChain<TUserId>.Create(
                    ChainId.New(),
                    context.TenantId,
                    context.UserId,
                    root.SecurityVersion,
                    claimsSnapshot);

                root = root.AttachChain(chain, now);

                // Session
                var session = UAuthSession<TUserId>.Create(
                    sessionId: new AuthSessionId(opaqueSessionId),
                    tenantId: context.TenantId,
                    userId: context.UserId,
                    chainId: chain.ChainId,
                    now: now,
                    expiresAt: expiresAt,
                    claims: context.Claims,
                    device: context.DeviceInfo,
                    metadata: context.Metadata
                );

                // Persist (order is intentional)
                await store.SaveSessionRootAsync(context.TenantId, root);
                await store.SaveChainAsync(context.TenantId, chain);
                await store.SaveSessionAsync(context.TenantId, session);
                await store.SetActiveSessionIdAsync(
                    context.TenantId,
                    chain.ChainId,
                    session.SessionId);

                issued = new IssuedSession<TUserId>
                {
                    Session = session,
                    OpaqueSessionId = opaqueSessionId,
                    IsMetadataOnly = _options.Mode == UAuthMode.SemiHybrid
                };
            });

            //if (httpContext is not null)
            //{
            //    _cookieManager.Issue(httpContext, opaqueSessionId);
            //}

            return issued!;
        }

        public Task<IssuedSession<TUserId>> RotateSessionAsync(SessionRotationContext<TUserId> context, CancellationToken ct = default)
        {
            return RotateInternalAsync(httpContext: null, context, ct);
        }

        public Task<IssuedSession<TUserId>> RotateSessionAsync(HttpContext httpContext, SessionRotationContext<TUserId> context, CancellationToken ct = default)
        {
            if (httpContext is null)
                throw new ArgumentNullException(nameof(httpContext));

            return RotateInternalAsync(httpContext, context, ct);
        }

        private async Task<IssuedSession<TUserId>> RotateInternalAsync(HttpContext httpContext, SessionRotationContext<TUserId> context, CancellationToken ct = default)
        {
            var now = context.Now;
            var store = _storeFactory.Create<TUserId>(context.TenantId);

            IssuedSession<TUserId>? issued = null;

            await store.ExecuteAsync(async () =>
            {
                var session = await store.GetSessionAsync(
                    context.TenantId,
                    context.CurrentSessionId);

                if (session is null)
                    throw new SecurityException("Session not found.");

                if (session.IsRevoked || session.ExpiresAt <= now)
                    throw new SecurityException("Session is no longer valid.");

                var chainId = session.ChainId;

                var chain = await store.GetChainAsync(
                    context.TenantId,
                    chainId);

                if (chain is null || chain.IsRevoked)
                    throw new SecurityException("Session chain is invalid.");

                var opaqueSessionId = _opaqueGenerator.Generate();

                var expiresAt = now.Add(_options.Session.Lifetime);

                if (_options.Session.MaxLifetime is not null)
                {
                    var absoluteExpiry = now.Add(_options.Session.MaxLifetime.Value);
                    if (absoluteExpiry < expiresAt)
                        expiresAt = absoluteExpiry;
                }

                var newSession = UAuthSession<TUserId>.Create(
                    sessionId: new AuthSessionId(opaqueSessionId),
                    tenantId: session.TenantId,
                    userId: session.UserId,
                    chainId: chain.ChainId,
                    now: now,
                    expiresAt: expiresAt,
                    claims: chain.ClaimsSnapshot,
                    device: session.Device,
                    metadata: session.Metadata
                );

                await store.SaveSessionAsync(context.TenantId, newSession);

                var rotatedChain = chain.RotateSession(newSession.SessionId);

                await store.SaveChainAsync(context.TenantId, rotatedChain);
                await store.SetActiveSessionIdAsync(
                    context.TenantId,
                    chain.ChainId,
                    newSession.SessionId);

                await store.RevokeSessionAsync(
                    context.TenantId,
                    session.SessionId,
                    now);

                issued = new IssuedSession<TUserId>
                {
                    Session = newSession,
                    OpaqueSessionId = opaqueSessionId,
                    IsMetadataOnly = _options.Mode == UAuthMode.SemiHybrid
                };
            });

            if (httpContext is not null)
            {
                _cookieManager.Issue(httpContext, issued!.OpaqueSessionId);
            }

            return issued!;
        }

        public async Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
        {
            var store = _storeFactory.Create<TUserId>(tenantId);

            await store.ExecuteAsync(async () =>
            {
                var session = await store.GetSessionAsync(tenantId, sessionId);
                if (session is null)
                    return;

                if (session.IsRevoked)
                    return;

                await store.RevokeSessionAsync(
                    tenantId,
                    sessionId,
                    at.UtcDateTime);
            });
        }

        public async Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTimeOffset at, CancellationToken ct = default)
        {
            var store = _storeFactory.Create<TUserId>(tenantId);

            await store.ExecuteAsync(async () =>
            {
                var chain = await store.GetChainAsync(tenantId, chainId);
                if (chain is null)
                    return;

                if (chain.IsRevoked)
                    return;

                await store.RevokeChainAsync(tenantId, chainId, at.UtcDateTime);

                if (chain.ActiveSessionId is not null)
                {
                    await store.RevokeSessionAsync(tenantId, chain.ActiveSessionId.Value, at.UtcDateTime);
                }
            });
        }

        public async Task RevokeAllChainsAsync(string? tenantId, TUserId userId, ChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default)
        {
            var store = _storeFactory.Create<TUserId>(tenantId);

            await store.ExecuteAsync(async () =>
            {
                var root = await store.GetSessionRootAsync(tenantId, userId);
                if (root is null)
                    return;

                foreach (var chain in root.Chains)
                {
                    if (exceptChainId.HasValue && chain.ChainId.Equals(exceptChainId.Value))
                    {
                        continue;
                    }

                    await store.RevokeChainAsync(tenantId, chain.ChainId, at.UtcDateTime);

                    if (chain.ActiveSessionId is not null)
                    {
                        await store.RevokeSessionAsync(tenantId, chain.ActiveSessionId.Value, at.UtcDateTime);
                    }
                }

                await store.SaveSessionRootAsync(tenantId, root);
            });
        }

        public async Task RevokeRootAsync(string? tenantId, TUserId userId, DateTimeOffset at, CancellationToken ct = default)
        {
            var store = _storeFactory.Create<TUserId>(tenantId);

            await store.ExecuteAsync(async () =>
            {
                var root = await store.GetSessionRootAsync(tenantId, userId);
                if (root is null)
                    return;

                var revokedRoot = root.Revoke(at);

                await store.SaveSessionRootAsync(tenantId, revokedRoot);

                foreach (var chain in root.Chains)
                {
                    await store.RevokeChainAsync(tenantId, chain.ChainId, at);

                    if (chain.ActiveSessionId is not null)
                    {
                        await store.RevokeSessionAsync(
                            tenantId,
                            chain.ActiveSessionId.Value,
                            at);
                    }
                }
            });
        }

    }
}
