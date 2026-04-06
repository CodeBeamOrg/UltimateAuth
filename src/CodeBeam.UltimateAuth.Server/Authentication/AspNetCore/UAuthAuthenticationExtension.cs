using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Authentication;

namespace CodeBeam.UltimateAuth.Server.Authentication;

public static class UAuthAuthenticationExtensions
{
    public static AuthenticationBuilder AddUAuthScheme(this AuthenticationBuilder builder, Action<UAuthAuthenticationSchemeOptions>? configure = null)
    {
        return builder.AddScheme<UAuthAuthenticationSchemeOptions, UAuthAuthenticationHandler>(UAuthConstants.SchemeDefaults.GlobalScheme,
            options =>
            {
                configure?.Invoke(options);
            });
    }

    public static AuthenticationBuilder AddUAuthResourceApi(this AuthenticationBuilder builder)
    {
        return builder.AddScheme<UAuthAuthenticationSchemeOptions, UAuthResourceAuthenticationHandler>(
            UAuthConstants.SchemeDefaults.GlobalScheme,
            options => { });
    }
}
