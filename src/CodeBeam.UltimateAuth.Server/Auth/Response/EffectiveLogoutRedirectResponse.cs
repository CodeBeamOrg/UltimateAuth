namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed record EffectiveLogoutRedirectResponse
(
    bool RedirectEnabled,
    string RedirectPath,
    bool AllowReturnUrlOverride
);
