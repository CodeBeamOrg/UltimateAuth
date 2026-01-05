using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;

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
        private readonly IClock _clock;

        public UAuthTokenIssuer(IOpaqueTokenGenerator opaqueGenerator, IJwtTokenGenerator jwtGenerator, ITokenHasher tokenHasher, IClock clock)
        {
            _opaqueGenerator = opaqueGenerator;
            _jwtGenerator = jwtGenerator;
            _tokenHasher = tokenHasher;
            _clock = clock;
        }

        public Task<AccessToken> IssueAccessTokenAsync(AuthFlowContext flow, TokenIssuanceContext context, CancellationToken ct = default)
        {
            var tokens = flow.OriginalOptions.Tokens;
            var now = _clock.UtcNow;
            var expires = now.Add(tokens.AccessTokenLifetime);

            return flow.EffectiveMode switch
            {
                UAuthMode.PureOpaque or UAuthMode.Hybrid =>
                    Task.FromResult(IssueOpaqueAccessToken(expires, context.SessionId)),

                UAuthMode.SemiHybrid or
                UAuthMode.PureJwt =>
                    Task.FromResult(IssueJwtAccessToken(context, tokens, expires)),

                _ => throw new InvalidOperationException(
                    $"Unsupported auth mode: {flow.EffectiveMode}")
            };
        }

        public Task<RefreshToken?> IssueRefreshTokenAsync(AuthFlowContext flow, TokenIssuanceContext context, CancellationToken ct = default)
        {
            if (flow.EffectiveMode == UAuthMode.PureOpaque)
                return Task.FromResult<RefreshToken?>(null);

            var tokens = flow.OriginalOptions.Tokens;
            var expires = _clock.UtcNow.Add(tokens.RefreshTokenLifetime);

            var raw = _opaqueGenerator.Generate();
            var hash = _tokenHasher.Hash(raw);

            return Task.FromResult<RefreshToken?>(new RefreshToken
            {
                Token = raw,
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

        private AccessToken IssueJwtAccessToken(TokenIssuanceContext context, UAuthTokenOptions tokens, DateTimeOffset expires)
        {
            var claims = new Dictionary<string, object>
            {
                ["sub"] = context.UserId,
                ["tenant"] = context.TenantId
            };

            foreach (var kv in context.Claims)
                claims[kv.Key] = kv.Value;

            if (!string.IsNullOrWhiteSpace(context.SessionId))
                claims["sid"] = context.SessionId!;

            if (tokens.AddJwtIdClaim)
                claims["jti"] = _opaqueGenerator.Generate(16);

            var descriptor = new UAuthJwtTokenDescriptor
            {
                Subject = context.UserId,
                Issuer = tokens.Issuer,
                Audience = tokens.Audience,
                IssuedAt = _clock.UtcNow,
                ExpiresAt = expires,
                TenantId = context.TenantId,
                Claims = claims,
                KeyId = tokens.KeyId
            };

            var jwt = _jwtGenerator.CreateToken(descriptor);

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
