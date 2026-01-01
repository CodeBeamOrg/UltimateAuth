using Microsoft.AspNetCore.Authentication;

namespace CodeBeam.UltimateAuth.Server.Authentication;

public static class UAuthAuthenticationExtensions
{
    public static AuthenticationBuilder AddUAuthCookies(this AuthenticationBuilder builder)
    {
        return builder.AddScheme<UAuthCookieOptions, UAuthAuthenticationHandler>(UAuthCookieDefaults.AuthenticationScheme,
            _ => { });
    }
}
