using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Issuers
{
    /// <summary>
    /// Default UltimateAuth token issuer.
    /// Opinionated implementation of ITokenIssuer.
    /// Mode-aware (PureOpaque, Hybrid, SemiHybrid, PureJwt).
    /// </summary>
    public sealed class UAuthTokenIssuer : ITokenIssuer
    {
        private readonly IOpaqueTokenGenerator _opaqueGenerator;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly ITokenHasher _tokenHasher;
        private readonly UAuthServerOptions _options;
        private readonly IClock _clock;

        public UAuthTokenIssuer(IOpaqueTokenGenerator opaqueGenerator, IJwtTokenGenerator jwtGenerator, ITokenHasher tokenHasher, IOptions<UAuthServerOptions> options, IClock clock)
        {
            _opaqueGenerator = opaqueGenerator;
            _jwtGenerator = jwtGenerator;
            _tokenHasher = tokenHasher;
            _options = options.Value;
            _clock = clock;
        }

        public Task<AccessToken> IssueAccessTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var expires = now.Add(_options.Tokens.AccessTokenLifetime);

            return _options.Mode switch
            {
                UAuthMode.PureOpaque => Task.FromResult(IssueOpaqueAccessToken(
                    expires,
                    context.SessionId)),

                UAuthMode.Hybrid or
                UAuthMode.SemiHybrid or
                UAuthMode.PureJwt => Task.FromResult(IssueJwtAccessToken(
                    context,
                    expires)),

                _ => throw new InvalidOperationException(
                    $"Unsupported auth mode: {_options.Mode}")
            };
        }

        public Task<RefreshToken?> IssueRefreshTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default)
        {
            if (_options.Mode == UAuthMode.PureOpaque)
                return Task.FromResult<RefreshToken?>(null);

            var now = DateTimeOffset.UtcNow;
            var expires = now.Add(_options.Tokens.RefreshTokenLifetime);

            string token = _opaqueGenerator.Generate();
            string hash = _tokenHasher.Hash(token);

            return Task.FromResult<RefreshToken?>(new RefreshToken
            {
                Token = token,
                TokenHash = hash,
                ExpiresAt = expires
            });
        }

        private AccessToken IssueOpaqueAccessToken(DateTimeOffset expires, string? sessionId)
        {
            string token = _opaqueGenerator.Generate();

            return new AccessToken
            {
                Token = token,
                Type = TokenType.Opaque,
                ExpiresAt = expires,
                SessionId = sessionId
            };
        }

        private AccessToken IssueJwtAccessToken(TokenIssuanceContext context, DateTimeOffset expires)
        {
            var claims = new Dictionary<string, object>
            {
                ["sub"] = context.UserId,
                ["tenant"] = context.TenantId
            };

            foreach (var kv in context.Claims)
            {
                claims[kv.Key] = kv.Value;
            }

            if (!string.IsNullOrWhiteSpace(context.SessionId))
            {
                claims["sid"] = context.SessionId!;
            }

            if (_options.Tokens.AddJwtIdClaim)
            {
                claims["jti"] = _opaqueGenerator.Generate(16);
            }

            var descriptor = new UAuthJwtTokenDescriptor
            {
                Subject = context.UserId,
                Issuer = _options.Tokens.Issuer,
                Audience = _options.Tokens.Audience,
                IssuedAt = _clock.UtcNow,
                ExpiresAt = expires,
                TenantId = context.TenantId,
                Claims = claims,
                KeyId = _options.Tokens.KeyId
            };

            string jwt = _jwtGenerator.CreateToken(descriptor);

            return new AccessToken
            {
                Token = jwt,
                Type = TokenType.Jwt,
                ExpiresAt = expires,
                SessionId = context.SessionId
            };
        }

    }
}
