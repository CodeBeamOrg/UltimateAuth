using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed record EffectiveAuthResponse(
    CredentialResponseOptions SessionIdDelivery,
    CredentialResponseOptions AccessTokenDelivery,
    CredentialResponseOptions RefreshTokenDelivery,
    EffectiveLoginRedirectResponse Login,
    EffectiveLogoutRedirectResponse Logout
);
