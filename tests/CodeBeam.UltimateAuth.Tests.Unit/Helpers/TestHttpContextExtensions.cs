using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestHttpContextExtensions
{
    public static HttpContext WithQuery(this HttpContext ctx, string key, string value)
    {
        ctx.Request.QueryString = QueryString.Create(key, value);
        return ctx;
    }

    public static HttpContext WithHeader(this HttpContext ctx, string name, string value)
    {
        ctx.Request.Headers[name] = value;
        return ctx;
    }

    public static HttpContext WithForm(this HttpContext ctx, IDictionary<string, string> form)
    {
        ctx.Request.ContentType = "application/x-www-form-urlencoded";
        ctx.Request.Form = new FormCollection(
            form.ToDictionary(
                x => x.Key,
                x => new Microsoft.Extensions.Primitives.StringValues(x.Value)
            )
        );
        return ctx;
    }

    public static HttpContext WithReturnUrl(this HttpContext ctx, string returnUrl)
    {
        return ctx.WithForm(new Dictionary<string, string>
        {
            ["return_url"] = returnUrl
        });
    }
}
