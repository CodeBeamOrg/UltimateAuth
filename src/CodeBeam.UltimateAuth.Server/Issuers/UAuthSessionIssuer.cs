using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security;

namespace CodeBeam.UltimateAuth.Server.Issuers
{
    public sealed class UAuthSessionIssuer : IHttpSessionIssuer
    {
        private readonly ISessionStore _sessionStore;
        private readonly IOpaqueTokenGenerator _opaqueGenerator;
        private readonly UAuthServerOptions _options;
        private readonly IUAuthCookieManager _cookieManager;

        public UAuthSessionIssuer(ISessionStore sessionStore, IOpaqueTokenGenerator opaqueGenerator, IOptions<UAuthServerOptions> options, IUAuthCookieManager cookieManager)
        {
            _sessionStore = sessionStore;
            _opaqueGenerator = opaqueGenerator;
            _options = options.Value;
            _cookieManager = cookieManager;
        }

        public Task<IssuedSession> IssueLoginSessionAsync(AuthenticatedSessionContext context, CancellationToken ct = default)
        {
            return IssueLoginInternalAsync(httpContext: null, context, ct);
        }

        public Task<IssuedSession> IssueLoginSessionAsync(HttpContext httpContext, AuthenticatedSessionContext context, CancellationToken ct = default)
        {
            if (httpContext is null)
                throw new ArgumentNullException(nameof(httpContext));

            return IssueLoginInternalAsync(httpContext, context, ct);
        }

        private async Task<IssuedSession> IssueLoginInternalAsync(HttpContext? httpContext, AuthenticatedSessionContext context, CancellationToken ct = default)
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
                tenantId: context.TenantId,
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

            await _sessionStore.CreateSessionAsync(issued,
                new SessionStoreContext
                {
                    TenantId = context.TenantId,
                    UserKey = context.UserKey,
                    ChainId = context.ChainId,
                    IssuedAt = now,
                    Device = context.Device
                },
                ct
            );

            return issued;
        }

        public Task<IssuedSession> RotateSessionAsync(SessionRotationContext context, CancellationToken ct = default)
        {
            return RotateInternalAsync(httpContext: null, context, ct);
        }

        public Task<IssuedSession> RotateSessionAsync(HttpContext httpContext, SessionRotationContext context, CancellationToken ct = default)
        {
            if (httpContext is null)
                throw new ArgumentNullException(nameof(httpContext));

            return RotateInternalAsync(httpContext, context, ct);
        }

        private async Task<IssuedSession> RotateInternalAsync(HttpContext httpContext, SessionRotationContext context, CancellationToken ct = default)
        {
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
                    tenantId: context.TenantId,
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

            await _sessionStore.RotateSessionAsync(context.CurrentSessionId, issued,
                new SessionStoreContext
                {
                    TenantId = context.TenantId,
                    UserKey = context.UserKey,
                    IssuedAt = now,
                    Device = context.Device,
                },
                ct
            );

            return issued;
        }

        public async Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
        {
            await _sessionStore.RevokeSessionAsync(tenantId, sessionId, at, ct );
        }

        public async Task RevokeChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
        {
            await _sessionStore.RevokeChainAsync(tenantId, chainId, at, ct );
        }

        public async Task RevokeAllChainsAsync(string? tenantId, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default)
        {
            await _sessionStore.RevokeAllChainsAsync(tenantId, userKey, exceptChainId, at, ct );
        }

        public async Task RevokeRootAsync(string? tenantId, UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
        {
            await _sessionStore.RevokeRootAsync(tenantId, userKey, at, ct );
        }

    }
}
