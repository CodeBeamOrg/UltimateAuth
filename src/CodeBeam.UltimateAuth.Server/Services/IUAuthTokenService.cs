using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services
{
    /// <summary>
    /// Issues, refreshes and validates access and refresh tokens.
    /// Stateless or hybrid depending on auth mode.
    /// </summary>
    public interface IUAuthTokenService<TUserId>
    {
        /// <summary>
        /// Issues access (and optionally refresh) tokens
        /// for a validated session.
        /// </summary>
        Task<AuthTokens> CreateTokensAsync(AuthFlowContext flow, TokenIssueContext<TUserId> context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes tokens using a refresh token.
        /// </summary>
        Task<AuthTokens> RefreshAsync(AuthFlowContext flow, TokenRefreshContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates JWT.
        /// </summary>
        Task<TokenValidationResult<TUserId>> ValidateJwtAsync(string token, CancellationToken cancellationToken = default);
    }
}
