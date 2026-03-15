using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Abstactions;

/// <summary>
/// Issues access and refresh tokens according to the active auth mode.
/// Does not perform persistence or validation.
/// </summary>
public interface ITokenIssuer
{
    Task<AccessToken> IssueAccessTokenAsync(AuthFlowContext flow, TokenIssuanceContext context, CancellationToken cancellationToken = default);
    Task<RefreshTokenInfo?> IssueRefreshTokenAsync(AuthFlowContext flow, TokenIssuanceContext context, RefreshTokenPersistence persistence, CancellationToken cancellationToken = default);
}
