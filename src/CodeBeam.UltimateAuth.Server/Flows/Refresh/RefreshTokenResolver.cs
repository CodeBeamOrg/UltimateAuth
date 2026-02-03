using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class RefreshTokenResolver : IRefreshTokenResolver
{
    private const string DefaultCookieName = "uar";
    private const string BearerPrefix = "Bearer ";
    private const string RefreshHeaderName = "X-Refresh-Token";

    public string? Resolve(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(DefaultCookieName, out var cookieToken) &&
            !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        if (context.Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
        {
            var value = authHeader.ToString();
            if (value.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var token = value.Substring(BearerPrefix.Length).Trim();
                if (!string.IsNullOrWhiteSpace(token))
                    return token;
            }
        }

        if (context.Request.Headers.TryGetValue(RefreshHeaderName, out var headerToken) &&
            !string.IsNullOrWhiteSpace(headerToken))
        {
            return headerToken.ToString();
        }

        return null;
    }
}
