using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Security;

internal static class UAuthCsrfResponseInspector
{
    public static bool CanIssueAuthenticationCookie(EffectiveAuthResponse response)
    {
        return response.SessionIdDelivery.Mode == GrantDeliveryMode.Cookie
               || response.AccessTokenDelivery.Mode == GrantDeliveryMode.Cookie
               || response.RefreshTokenDelivery.Mode == GrantDeliveryMode.Cookie;
    }
}
