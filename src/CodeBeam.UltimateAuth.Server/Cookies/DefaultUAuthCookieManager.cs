using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Cookies;

internal sealed class DefaultUAuthCookieManager : IUAuthCookieManager
{
    private readonly UAuthCookieOptionsBuilder _builder;

    public DefaultUAuthCookieManager(UAuthCookieOptionsBuilder builder)
    {
        _builder = builder;
    }

    public void Write(
        HttpContext ctx,
        string value,
        UAuthCookieOptions options,
        TimeSpan? logicalLifetime)
    {
        var cookieOptions = _builder.Build(options, logicalLifetime);

        ctx.Response.Cookies.Append(
            options.Name,
            value,
            cookieOptions);
    }

    public bool TryRead(
        HttpContext ctx,
        string cookieName,
        out string value)
    {
        return ctx.Request.Cookies.TryGetValue(cookieName, out value!);
    }

    public void Delete(
        HttpContext ctx,
        UAuthCookieOptions options)
    {
        ctx.Response.Cookies.Delete(options.Name);
    }
}
