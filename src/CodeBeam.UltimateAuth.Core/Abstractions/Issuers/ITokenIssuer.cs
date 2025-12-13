using CodeBeam.UltimateAuth.Core.Contexts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Issues access and refresh tokens according to the active auth mode.
    /// Does not perform persistence or validation.
    /// </summary>
    public interface ITokenIssuer
    {
        Task<IssuedAccessToken> IssueAccessTokenAsync(TokenIssueContext context, CancellationToken cancellationToken = default);
        Task<IssuedRefreshToken?> IssueRefreshTokenAsync(TokenIssueContext context, CancellationToken cancellationToken = default);
    }
}
