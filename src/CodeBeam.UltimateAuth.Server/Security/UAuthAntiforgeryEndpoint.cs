using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Security;

internal static class UAuthAntiforgeryEndpoint
{
    public static async Task<IResult> GetToken(HttpContext context, IAntiforgery antiforgery)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Explicitly authenticate using UltimateAuth's own scheme.
        // This ensures antiforgery uses the same principal as authenticated mutation endpoints.
        var authentication = await context.AuthenticateAsync(UAuthConstants.SchemeDefaults.GlobalScheme);

        if (authentication.Succeeded && authentication.Principal is not null)
        {
            context.User = authentication.Principal;
        }
        else if (context.Request.Headers.ContainsKey("Authorization"))
        {
            // Never issue an anonymous antiforgery token when the caller supplied an invalid access credential.
            return Results.Unauthorized();
        }

        var tokens = antiforgery.GetAndStoreTokens(context);

        return Results.Ok(new
        {
            requestToken = tokens.RequestToken,
            headerName = tokens.HeaderName,
            formFieldName = tokens.FormFieldName
        });
    }
}
