using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal static class UAuthLoginTransportEndpoint
{
    public static IResult Get(HttpContext context, IAuthFlowContextAccessor accessor)
    {
        var response = accessor.Current.Response;

        var sessionHeader = response.SessionIdDelivery.Mode == GrantDeliveryMode.Header;

        var accessHeader = response.AccessTokenDelivery.Mode == GrantDeliveryMode.Header;

        context.Response.Headers.CacheControl = "no-store";

        return Results.Ok(new
        {
            // The current client does not manage header-delivered opaque session IDs.
            Supported = !sessionHeader,

            // Native navigation cannot capture response headers.
            NavigationSupported = !sessionHeader && !accessHeader,

            AccessTokenInHeader = accessHeader
        });
    }
}
