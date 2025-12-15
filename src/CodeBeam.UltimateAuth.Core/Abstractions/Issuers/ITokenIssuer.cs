using CodeBeam.UltimateAuth.Core.Contexts;
using CodeBeam.UltimateAuth.Core.Models;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Issues access and refresh tokens according to the active auth mode.
    /// Does not perform persistence or validation.
    /// </summary>
    public interface ITokenIssuer
    {
        Task<AccessToken> IssueAccessTokenAsync(TokenIssuerContext context, CancellationToken cancellationToken = default);
        Task<RefreshToken?> IssueRefreshTokenAsync(TokenIssuerContext context, CancellationToken cancellationToken = default);
    }
}
