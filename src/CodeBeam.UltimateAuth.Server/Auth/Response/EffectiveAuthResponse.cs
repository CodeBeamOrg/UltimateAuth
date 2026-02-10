using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed class EffectiveAuthResponse
{
    public CredentialResponseOptions SessionIdDelivery { get; }
    public CredentialResponseOptions AccessTokenDelivery { get; }
    public CredentialResponseOptions RefreshTokenDelivery { get; }
    public EffectiveRedirectResponse Redirect { get; }

    public EffectiveAuthResponse(
        CredentialResponseOptions sessionIdDelivery,
        CredentialResponseOptions accessTokenDelivery,
        CredentialResponseOptions refreshTokenDelivery,
        EffectiveRedirectResponse redirect)
    {
        SessionIdDelivery = sessionIdDelivery;
        AccessTokenDelivery = accessTokenDelivery;
        RefreshTokenDelivery = refreshTokenDelivery;
        Redirect = redirect;
    }
}
