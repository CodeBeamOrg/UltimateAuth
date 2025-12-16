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

        public UAuthTokenIssuer(IOpaqueTokenGenerator opaqueGenerator, IJwtTokenGenerator jwtGenerator, ITokenHasher tokenHasher, IOptions<UAuthServerOptions> options)
        {
            _opaqueGenerator = opaqueGenerator;
            _jwtGenerator = jwtGenerator;
            _tokenHasher = tokenHasher;
            _options = options.Value;
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
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, context.UserId),
                new Claim("tenant", context.TenantId)
            };

            claims.AddRange(context.Claims);

            if (!string.IsNullOrWhiteSpace(context.SessionId))
            {
                claims.Add(new Claim("sid", context.SessionId));
            }

            if (_options.Tokens.AddJwtIdClaim)
            {
                string jti = _opaqueGenerator.Generate(16); // shorter is fine
                claims.Add(new Claim("jti", jti));
            }

            var descriptor = new UAuthJwtTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _options.Tokens.Issuer,
                Audience = _options.Tokens.Audience,
                Expires = expires.UtcDateTime,
                SigningKey = _options.Tokens.SigningKey
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
