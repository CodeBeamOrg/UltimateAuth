using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions;

internal static class HttpContextReturnUrlExtensions
{
    public static string? GetReturnUrl(this HttpContext ctx)
    {
        if (ctx.Request.HasFormContentType && ctx.Request.Form.TryGetValue("return_url", out var form))
        {
            return form.ToString();
        }

        if (ctx.Request.Query.TryGetValue("return_url", out var query))
        {
            return query.ToString();
        }

        return null;
    }
}
