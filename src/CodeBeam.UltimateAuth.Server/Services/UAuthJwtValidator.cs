using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthJwtValidator : IJwtValidator
    {
        private readonly JsonWebTokenHandler _jwtHandler;
        private readonly TokenValidationParameters _jwtParameters;
        private readonly IUserIdConverterResolver _converters;

        public UAuthJwtValidator(
            TokenValidationParameters jwtParameters,
            IUserIdConverterResolver converters)
        {
            _jwtHandler = new JsonWebTokenHandler();
            _jwtParameters = jwtParameters;
            _converters = converters;
        }

        public async Task<TokenValidationResult<TUserId>> ValidateAsync<TUserId>(string token, CancellationToken ct = default)
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
            if (!AuthSessionId.TryCreate(sid, out AuthSessionId ssid))
            {
                sessionId = ssid;
            }

            return TokenValidationResult<TUserId>.Valid(
                type: TokenType.Jwt,
                tenantId: tenantId,
                userId,
                sessionId: sessionId,
                claims: claims,
                expiresAt: jwt.ValidTo);
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
