using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Security;

internal static class UAuthAntiforgeryEndpoint
{
    public static IResult GetToken(HttpContext context, IAntiforgery antiforgery)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache";

        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        var tokens = antiforgery.GetAndStoreTokens(context);

        return Results.Ok(new
        {
            requestToken = tokens.RequestToken,
            headerName = tokens.HeaderName,
            formFieldName = tokens.FormFieldName
        });
    }
}
