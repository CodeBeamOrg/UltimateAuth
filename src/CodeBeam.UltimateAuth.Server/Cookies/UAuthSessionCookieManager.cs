using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Cookies;

internal sealed class UAuthSessionCookieManager : IUAuthSessionCookieManager
{
    private readonly UAuthServerOptions _options;

    public UAuthSessionCookieManager(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value;
    }

    public void Issue(HttpContext context, string sessionId)
    {
        var cookieOptions = BuildCookieOptions(context);
        context.Response.Cookies.Append(_options.Cookie.Name, sessionId, cookieOptions);
    }

    public bool TryRead(HttpContext context, out string sessionId)
    {
        return context.Request.Cookies.TryGetValue(_options.Cookie.Name, out sessionId!);
    }

    public void Revoke(HttpContext context)
    {
        context.Response.Cookies.Delete(_options.Cookie.Name, BuildCookieOptions(context));
    }

    private CookieOptions BuildCookieOptions(HttpContext context)
    {
        return new CookieOptions
        {
            HttpOnly = _options.Cookie.HttpOnly,
            Secure = _options.Cookie.SecurePolicy == CookieSecurePolicy.Always,
            SameSite = ResolveSameSite(),
            Path = "/"
        };
    }

    private SameSiteMode ResolveSameSite()
    {
        if (_options.Cookie.SameSiteOverride is not null)
            return _options.Cookie.SameSiteOverride.Value;

        return _options.HubDeploymentMode switch
        {
            UAuthHubDeploymentMode.Embedded => SameSiteMode.Strict,
            UAuthHubDeploymentMode.Integrated => SameSiteMode.Lax,
            UAuthHubDeploymentMode.External => SameSiteMode.None,
            _ => SameSiteMode.Lax
        };
    }

}
