using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Options;

internal static class UAuthCookiePolicyResolver
{
    public static SameSiteMode ResolveSameSite(UAuthServerOptions options)
    {
        if (options.Cookie.SameSiteOverride is not null)
            return options.Cookie.SameSiteOverride.Value;

        return options.HubDeploymentMode switch
        {
            UAuthHubDeploymentMode.Embedded => SameSiteMode.Strict,
            UAuthHubDeploymentMode.Integrated => SameSiteMode.Lax,
            UAuthHubDeploymentMode.External => SameSiteMode.None,
            _ => throw new InvalidOperationException()
        };
    }
}
