using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Cookies;

internal sealed class UAuthSessionCookieManager : IUAuthCookieManager
{
    private readonly UAuthServerOptions _options;

    public UAuthSessionCookieManager(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value;
    }

    public void Write(HttpContext context, string value, Action<CookieOptions>? configure = null)
    {
        var options = BuildCookieOptions(context);
        configure?.Invoke(options);

        context.Response.Cookies.Append(_options.Cookie.Name, value, options);
    }

    public bool TryRead(HttpContext context, out string value)
    {
        return context.Request.Cookies.TryGetValue(_options.Cookie.Name, out value!);
    }

    public void Delete(HttpContext context)
    {
        context.Response.Cookies.Delete(_options.Cookie.Name);
    }

    private CookieOptions BuildCookieOptions(HttpContext context)
    {
        var cookie = _options.Cookie;
        var options = new CookieOptions
        {
            HttpOnly = cookie.HttpOnly,
            Secure = cookie.SecurePolicy == CookieSecurePolicy.Always,
            SameSite = ResolveSameSite(),
            Path = cookie.Path ?? "/"
        };

        var maxAge = ResolveCookieMaxAge();
        if (maxAge is not null)
        {
            options.MaxAge = maxAge;
        }

        return options;
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

    private TimeSpan? ResolveCookieMaxAge()
    {
        var cookie = _options.Cookie;
        var session = _options.Session;
        var tokens = _options.Tokens;

        if (cookie.MaxAge is not null)
            return cookie.MaxAge;

        if (tokens.IssueRefresh)
        {
            return tokens.RefreshTokenLifetime;
        }

        if (session.IdleTimeout is not null)
        {
            return session.IdleTimeout + cookie.IdleBuffer;
        }

        return null;
    }


}
