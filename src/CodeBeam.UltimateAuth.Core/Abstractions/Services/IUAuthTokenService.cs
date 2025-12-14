using CodeBeam.UltimateAuth.Core.Models;

namespace CodeBeam.UltimateAuth.Core.Abstractions
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
        Task<TokenIssueResult> IssueAsync(
            TokenIssueContext<TUserId> context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes tokens using a refresh token.
        /// </summary>
        Task<TokenIssueResult> RefreshAsync(
            TokenRefreshContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an access token (JWT or opaque).
        /// </summary>
        Task<TokenValidationResult<TUserId>> ValidateAsync(
            string token,
            TokenType type,
            CancellationToken cancellationToken = default);
    }
}
