using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthTokenService<TUserId> : IUAuthTokenService<TUserId>
    {
        private readonly ITokenIssuer _issuer;
        private readonly ITokenValidator _validator;
        private readonly IUserIdConverter<TUserId> _userIdConverter;

        public UAuthTokenService(ITokenIssuer issuer, ITokenValidator validator, IUserIdConverterResolver converterResolver)
        {
            _issuer = issuer;
            _validator = validator;
            _userIdConverter = converterResolver.GetConverter<TUserId>();
        }

        public async Task<AuthTokens> CreateTokensAsync(
            TokenIssueContext<TUserId> context,
            CancellationToken ct = default)
        {
            var issuerCtx = ToIssuerContext(context);

            var access = await _issuer.IssueAccessTokenAsync(issuerCtx, ct);
            var refresh = await _issuer.IssueRefreshTokenAsync(issuerCtx, ct);

            return new AuthTokens
            {
                AccessToken = access,
                RefreshToken = refresh
            };
        }

        public async Task<AuthTokens> RefreshAsync(
            TokenRefreshContext context,
            CancellationToken ct = default)
        {
            throw new NotImplementedException("Refresh flow will be implemented after refresh-token store & validation.");
        }

        public async Task<TokenValidationResult<TUserId>> ValidateAsync(
            string token,
            TokenType type,
            CancellationToken ct = default)
            => await _validator.ValidateAsync<TUserId>(token, type, ct);

        private TokenIssuanceContext ToIssuerContext(TokenIssueContext<TUserId> src)
        {
            return new TokenIssuanceContext
            {
                UserId = _userIdConverter.ToString(src.Session.UserId),
                TenantId = src.TenantId,
                SessionId = src.Session.SessionId,
                Claims = src.Session.Claims.AsDictionary()
            };
        }

    }
}
