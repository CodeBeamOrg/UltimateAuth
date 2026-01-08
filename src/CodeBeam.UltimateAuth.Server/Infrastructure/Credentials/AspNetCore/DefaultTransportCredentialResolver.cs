using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultTransportCredentialResolver : ITransportCredentialResolver
    {
        private readonly IOptionsMonitor<UAuthServerOptions> _server;

        public DefaultTransportCredentialResolver(IOptionsMonitor<UAuthServerOptions> server)
        {
            _server = server;
        }

        public TransportCredential? Resolve(HttpContext context)
        {
            var cookies = _server.CurrentValue.Cookie;

            // 1️⃣ Authorization header (Bearer)
            if (TryFromAuthorizationHeader(context, out var bearer))
                return bearer;

            // 2️⃣ Cookies (session / refresh / access)
            if (TryFromCookies(context, cookies, out var cookie))
                return cookie;

            // 3️⃣ Query (legacy / special flows)
            if (TryFromQuery(context, out var query))
                return query;

            // 4️⃣ Body (rare, but possible – PKCE / device flows)
            if (TryFromBody(context, out var body))
                return body;

            // 5️⃣ Hub / external authority
            if (TryFromHub(context, out var hub))
                return hub;

            return null;
        }

        // ---------- resolvers ----------

        // TODO: Make scheme configurable, shouldn't be hard coded
        private static bool TryFromAuthorizationHeader(HttpContext ctx, out TransportCredential credential)
        {
            credential = default!;

            if (!ctx.Request.Headers.TryGetValue("Authorization", out var header))
                return false;

            var value = header.ToString();
            if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return false;

            var token = value["Bearer ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            credential = new TransportCredential
            {
                Kind = TransportCredentialKind.AccessToken,
                Value = token,
                TenantId = ctx.GetTenantContext().TenantId,
                Device = ctx.GetDevice()
            };

            return true;
        }

        private static bool TryFromCookies(
        HttpContext ctx,
        UAuthCookieSetOptions cookieSet,
        out TransportCredential credential)
        {
            credential = default!;

            // Session cookie
            if (TryReadCookie(ctx, cookieSet.Session.Name, out var session))
            {
                credential = Build(ctx, TransportCredentialKind.Session, session);
                return true;
            }

            // Refresh token cookie
            if (TryReadCookie(ctx, cookieSet.RefreshToken.Name, out var refresh))
            {
                credential = Build(ctx, TransportCredentialKind.RefreshToken, refresh);
                return true;
            }

            // Access token cookie (optional)
            if (TryReadCookie(ctx, cookieSet.AccessToken.Name, out var access))
            {
                credential = Build(ctx, TransportCredentialKind.AccessToken, access);
                return true;
            }

            return false;
        }

        private static bool TryFromQuery(HttpContext ctx, out TransportCredential credential)
        {
            credential = default!;

            if (!ctx.Request.Query.TryGetValue("access_token", out var token))
                return false;

            var value = token.ToString();
            if (string.IsNullOrWhiteSpace(value))
                return false;

            credential = new TransportCredential
            {
                Kind = TransportCredentialKind.AccessToken,
                Value = value,
                TenantId = ctx.GetTenantContext().TenantId,
                Device = ctx.GetDevice()
            };

            return true;
        }

        private static bool TryFromBody(HttpContext ctx, out TransportCredential credential)
        {
            credential = default!;
            // intentionally empty for now
            // body parsing is expensive and opt-in later
            return false;
        }

        private static bool TryFromHub(HttpContext ctx, out TransportCredential credential)
        {
            credential = default!;
            // UAuthHub detection can live here later
            return false;
        }

        private static bool TryReadCookie(HttpContext ctx, string name, out string value)
        {
            value = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (!ctx.Request.Cookies.TryGetValue(name, out var raw))
                return false;

            raw = raw?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            value = raw;
            return true;
        }

        private static TransportCredential Build(HttpContext ctx, TransportCredentialKind kind, string value)
        => new()
        {
            Kind = kind,
            Value = value,
            TenantId = ctx.GetTenantContext().TenantId,
            Device = ctx.GetDevice()
        };

    }
}
