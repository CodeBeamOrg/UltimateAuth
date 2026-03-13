using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions;

internal static class HttpContextReturnUrlExtensions
{
    public static string? GetReturnUrl(this HttpContext ctx)
    {
        if (ctx.Request.HasFormContentType && ctx.Request.Form.TryGetValue(UAuthConstants.Form.ReturnUrl, out var form))
        {
            return form.ToString();
        }

        if (ctx.Request.Query.TryGetValue(UAuthConstants.Query.ReturnUrl, out var query))
        {
            return query.ToString();
        }

        return null;
    }
}
