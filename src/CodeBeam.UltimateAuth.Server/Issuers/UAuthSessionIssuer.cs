using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Domain.Session;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Issuers
{
    /// <summary>
    /// UltimateAuth session issuer responsible for creating
    /// opaque authentication sessions.
    /// </summary>
    public sealed class UAuthSessionIssuer<TUserId> : ISessionIssuer<TUserId>
    {
        private readonly IOpaqueTokenGenerator _opaqueGenerator;
        private readonly UAuthServerOptions _options;

        public UAuthSessionIssuer(IOpaqueTokenGenerator opaqueGenerator, IOptions<UAuthServerOptions> options)
        {
            _opaqueGenerator = opaqueGenerator;
            _options = options.Value;
        }

        // chain is intentionally provided for future policy extensions
        public Task<IssuedSession<TUserId>> IssueAsync(
        AuthenticatedSessionContext<TUserId> context,
        ISessionChain<TUserId> chain,
        CancellationToken cancellationToken = default)
        {
            if (_options.Mode == UAuthMode.PureJwt)
            {
                throw new InvalidOperationException(
                    "Session issuer cannot be used in PureJwt mode.");
            }

            var opaqueSessionId = _opaqueGenerator.Generate();
            var expiresAt = context.Now.Add(_options.Session.Lifetime);

            if (_options.Session.MaxLifetime is not null)
            {
                var absoluteExpiry =
                    context.Now.Add(_options.Session.MaxLifetime.Value);

                if (absoluteExpiry < expiresAt)
                    expiresAt = absoluteExpiry;
            }

            var session = UAuthSession<TUserId>.Create(
                sessionId: new AuthSessionId(opaqueSessionId),
                tenantId: context.TenantId,
                userId: context.UserId,
                now: context.Now,
                expiresAt: expiresAt,
                claims: context.Claims,
                device: context.DeviceInfo,
                metadata: context.Metadata
            );

            return Task.FromResult(new IssuedSession<TUserId>
            {
                Session = session,
                OpaqueSessionId = opaqueSessionId,
                IsMetadataOnly = _options.Mode == UAuthMode.SemiHybrid
            });
        }
    }
}
