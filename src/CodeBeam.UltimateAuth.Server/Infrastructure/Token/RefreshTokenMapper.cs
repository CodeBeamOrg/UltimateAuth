using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Contracts;

internal static class RefreshTokenMapper
{
    public static RefreshTokenInfo ToTransport(RefreshTokenIssuanceResult result)
    {
        return new RefreshTokenInfo
        {
            Token = result.Token,
            ExpiresAt = result.ExpiresAt
        };
    }
}
