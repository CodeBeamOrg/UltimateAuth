using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthNavigationOptions
{
    public bool EnableAutomaticNavigationRedirect { get; set; } = true;
    public Func<HttpContext, string>? LoginResolver { get; set; }
    public Func<HttpContext, string>? AccessDeniedResolver { get; set; }

    internal UAuthNavigationOptions Clone() => new()
    {
        EnableAutomaticNavigationRedirect = EnableAutomaticNavigationRedirect,
        LoginResolver = LoginResolver,
        AccessDeniedResolver = AccessDeniedResolver,
    };
}
