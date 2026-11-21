namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed class EffectiveAuthResponse
{
    public EffectiveGrantDelivery SessionIdDelivery { get; }
    public EffectiveGrantDelivery AccessTokenDelivery { get; }
    public EffectiveGrantDelivery RefreshTokenDelivery { get; }
    public EffectiveRedirectResponse Redirect { get; }

    public EffectiveAuthResponse(
        EffectiveGrantDelivery sessionIdDelivery,
        EffectiveGrantDelivery accessTokenDelivery,
        EffectiveGrantDelivery refreshTokenDelivery,
        EffectiveRedirectResponse redirect)
    {
        SessionIdDelivery = sessionIdDelivery;
        AccessTokenDelivery = accessTokenDelivery;
        RefreshTokenDelivery = refreshTokenDelivery;
        Redirect = redirect;
    }
}
