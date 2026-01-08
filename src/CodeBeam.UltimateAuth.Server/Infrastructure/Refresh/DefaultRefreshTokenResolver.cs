using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultRefreshTokenResolver : IRefreshTokenResolver
    {
        private const string DefaultCookieName = "ua_refresh";
        private const string BearerPrefix = "Bearer ";
        private const string RefreshHeaderName = "X-Refresh-Token";

        public string? Resolve(HttpContext context)
        {
            // 1️⃣ Cookie (preferred)
            if (context.Request.Cookies.TryGetValue(DefaultCookieName, out var cookieToken) &&
                !string.IsNullOrWhiteSpace(cookieToken))
            {
                return cookieToken;
            }

            // 2️⃣ Authorization: Bearer <token>
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

            // 3️⃣ Explicit header fallback
            if (context.Request.Headers.TryGetValue(RefreshHeaderName, out var headerToken) &&
                !string.IsNullOrWhiteSpace(headerToken))
            {
                return headerToken.ToString();
            }

            return null;
        }
    }
}
