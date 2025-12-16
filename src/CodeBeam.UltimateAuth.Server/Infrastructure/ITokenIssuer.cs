using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    /// <summary>
    /// Issues access and refresh tokens according to the active auth mode.
    /// Does not perform persistence or validation.
    /// </summary>
    public interface ITokenIssuer
    {
        Task<AccessToken> IssueAccessTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default);
        Task<RefreshToken?> IssueRefreshTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default);
    }
}
