using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Authentication;

namespace CodeBeam.UltimateAuth.Server.Authentication;

public static class UAuthAuthenticationExtensions
{
    public static AuthenticationBuilder AddUAuthCookies(this AuthenticationBuilder builder, Action<UAuthAuthenticationSchemeOptions>? configure = null)
    {
        return builder.AddScheme<UAuthAuthenticationSchemeOptions, UAuthAuthenticationHandler>(UAuthSchemeDefaults.AuthenticationScheme,
            options =>
            {
                configure?.Invoke(options);
            });
    }
}
