using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Security;

internal static class UAuthCsrfResponseInspector
{
    public static bool CanIssueAuthenticationCookie(EffectiveAuthResponse response)
    {
        return response.SessionIdDelivery.Mode == TokenResponseMode.Cookie
               || response.AccessTokenDelivery.Mode == TokenResponseMode.Cookie
               || response.RefreshTokenDelivery.Mode == TokenResponseMode.Cookie;
    }
}
