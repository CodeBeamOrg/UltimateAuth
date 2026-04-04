using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class UAuthCookieManager : IUAuthCookieManager
{
    public void Write(HttpContext context, string name, string value, CookieOptions options)
    {
        context.Response.Cookies.Append(name, value, options);
    }

    public bool TryRead(HttpContext context, string name, out string value)
    {
        return context.Request.Cookies.TryGetValue(name, out value!);
    }

    public void Delete(HttpContext context, string name)
    {
        context.Response.Cookies.Delete(name);
    }
}
