using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contexts;
using CodeBeam.UltimateAuth.Core.Models;

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

        public async Task<TokenIssueResult> IssueAsync(
            TokenIssueContext<TUserId> context,
            CancellationToken ct = default)
        {
            var issuerCtx = ToIssuerContext(context);

            var access = await _issuer.IssueAccessTokenAsync(issuerCtx, ct);
            var refresh = await _issuer.IssueRefreshTokenAsync(issuerCtx, ct);

            return TokenIssueResult.From(access, refresh);
        }

        public async Task<TokenIssueResult> RefreshAsync(
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

        private TokenIssueContext ToIssuerContext(TokenIssueContext<TUserId> src)
        {
            return new TokenIssueContext
            {
                UserId = _userIdConverter.ToString(src.UserId),
                TenantId = src.TenantId,
                SessionId = src.SessionId,
                Claims = src.Claims
            };
        }

    }
}
