using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class RefreshTokenResolver : IRefreshTokenResolver
{
    private readonly IAuthFlowContextAccessor _authContext;

    public RefreshTokenResolver(IAuthFlowContextAccessor authContext)
    {
        _authContext = authContext;
    }

    public string? Resolve(HttpContext context)
    {
        var flow = _authContext.Current;
        if (flow is null)
            return null;

        // Explicit refresh credential takes priority.
        var explicitToken = ReadHeader(context, "X-Refresh-Token");

        if (explicitToken is not null)
            return explicitToken;

        var delivery = flow.Response.RefreshTokenDelivery;

        if (delivery.Mode == TokenResponseMode.Cookie)
        {
            var cookieName = delivery.Cookie?.Name;

            if (!string.IsNullOrWhiteSpace(cookieName) &&
                context.Request.Cookies.TryGetValue(cookieName, out var cookieToken) &&
                !string.IsNullOrWhiteSpace(cookieToken))
            {
                return cookieToken;
            }
        }

        // Explicitly configured refresh-token response header.
        if (delivery.Mode == TokenResponseMode.Header && !string.IsNullOrWhiteSpace(delivery.Name))
        {
            return ReadHeader(context, delivery.Name);
        }

        return null;
    }

    private static string? ReadHeader(HttpContext context, string name)
    {
        if (!context.Request.Headers.TryGetValue(name, out var values))
            return null;

        var value = values.ToString().Trim();

        if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            value = value["Bearer ".Length..].Trim();
        }

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }
}
