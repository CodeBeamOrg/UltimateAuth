using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthTokenValidator : ITokenValidator
    {
        private readonly IOpaqueTokenStore _opaqueStore;
        private readonly JsonWebTokenHandler _jwtHandler;
        private readonly TokenValidationParameters _jwtParameters;
        private readonly IUserIdConverterResolver _converters;
        private readonly UAuthServerOptions _options;
        private readonly ITokenHasher _tokenHasher;

        public UAuthTokenValidator(
            IOpaqueTokenStore opaqueStore,
            TokenValidationParameters jwtParameters,
            IUserIdConverterResolver converters,
            IOptions<UAuthServerOptions> options,
            ITokenHasher tokenHasher)
        {
            _opaqueStore = opaqueStore;
            _jwtHandler = new JsonWebTokenHandler();
            _jwtParameters = jwtParameters;
            _converters = converters;
            _options = options.Value;
            _tokenHasher = tokenHasher;
        }

        public async Task<TokenValidationResult<TUserId>> ValidateAsync<TUserId>(
            string token,
            TokenType type,
            CancellationToken ct = default)
        {
            return type switch
            {
                TokenType.Jwt => await ValidateJwt<TUserId>(token),
                TokenType.Opaque => await ValidateOpaqueAsync<TUserId>(token, ct),
                _ => TokenValidationResult<TUserId>.Invalid(TokenType.Unknown, TokenInvalidReason.Unknown)
            };
        }

        // ---------------- JWT ----------------

        private async Task<TokenValidationResult<TUserId>> ValidateJwt<TUserId>(string token)
        {
            var result = await _jwtHandler.ValidateTokenAsync(token, _jwtParameters);

            if (!result.IsValid)
            {
                return TokenValidationResult<TUserId>.Invalid(TokenType.Jwt, MapJwtError(result.Exception));
            }

            var jwt = (JsonWebToken)result.SecurityToken;
            var claims = jwt.Claims.ToArray();

            var converter = _converters.GetConverter<TUserId>();

            var userIdString = jwt.GetClaim(ClaimTypes.NameIdentifier)?.Value ?? jwt.GetClaim("sub")?.Value;
            if (string.IsNullOrWhiteSpace(userIdString))
            {
                return TokenValidationResult<TUserId>.Invalid(TokenType.Jwt, TokenInvalidReason.MissingSubject);
            }

            TUserId userId;
            try
            {
                userId = converter.FromString(userIdString);
            }
            catch
            {
                return TokenValidationResult<TUserId>.Invalid(TokenType.Jwt, TokenInvalidReason.Malformed);
            }

            var tenantId = jwt.GetClaim("tenant")?.Value ?? jwt.GetClaim("tid")?.Value;
            AuthSessionId? sessionId = null;
            var sid = jwt.GetClaim("sid")?.Value;
            if (!string.IsNullOrWhiteSpace(sid))
            {
                sessionId = new AuthSessionId(sid);
            }

            return TokenValidationResult<TUserId>.Valid(
                type: TokenType.Jwt,
                tenantId: tenantId,
                userId,
                sessionId: sessionId,
                claims: claims,
                expiresAt: jwt.ValidTo);
        }


        // ---------------- OPAQUE ----------------

        private async Task<TokenValidationResult<TUserId>> ValidateOpaqueAsync<TUserId>(string token, CancellationToken ct)
        {
            var hash = _tokenHasher.Hash(token);

            var record = await _opaqueStore.FindByHashAsync(hash, ct);
            if (record is null)
            {
                return TokenValidationResult<TUserId>.Invalid(
                    TokenType.Opaque,
                    TokenInvalidReason.Invalid);
            }

            var now = DateTimeOffset.UtcNow;
            if (record.ExpiresAt <= now)
            {
                return TokenValidationResult<TUserId>.Invalid(
                    TokenType.Opaque,
                    TokenInvalidReason.Expired);
            }

            if (record.IsRevoked)
            {
                return TokenValidationResult<TUserId>.Invalid(
                    TokenType.Opaque,
                    TokenInvalidReason.Revoked);
            }

            var converter = _converters.GetConverter<TUserId>();

            TUserId userId;
            try
            {
                userId = converter.FromString(record.UserId);
            }
            catch
            {
                return TokenValidationResult<TUserId>.Invalid(
                    TokenType.Opaque,
                    TokenInvalidReason.Invalid);
            }

            return TokenValidationResult<TUserId>.Valid(
                TokenType.Opaque,
                record.TenantId,
                userId,
                record.SessionId,
                record.Claims,
                record.ExpiresAt.UtcDateTime);
        }

        private static TokenInvalidReason MapJwtError(Exception? ex)
        {
            return ex switch
            {
                SecurityTokenExpiredException => TokenInvalidReason.Expired,
                SecurityTokenInvalidSignatureException => TokenInvalidReason.SignatureInvalid,
                SecurityTokenInvalidAudienceException => TokenInvalidReason.AudienceMismatch,
                SecurityTokenInvalidIssuerException => TokenInvalidReason.IssuerMismatch,
                _ => TokenInvalidReason.Invalid
            };
        }

    }
}
