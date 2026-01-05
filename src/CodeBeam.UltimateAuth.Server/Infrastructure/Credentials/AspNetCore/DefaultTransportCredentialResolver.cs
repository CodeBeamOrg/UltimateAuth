using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultTransportCredentialResolver
    : ITransportCredentialResolver
    {
        public TransportCredential? Resolve(HttpContext context)
        {
            // 1️⃣ Authorization header (Bearer)
            if (TryFromAuthorizationHeader(context, out var bearer))
                return bearer;

            // 2️⃣ Cookies (session / refresh / access)
            if (TryFromCookies(context, out var cookie))
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

        private static bool TryFromAuthorizationHeader(
            HttpContext ctx,
            out TransportCredential credential)
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
            out TransportCredential credential)
        {
            credential = default!;

            // Session cookie (example name – configurable later)
            if (ctx.Request.Cookies.TryGetValue("uauth.session", out var session) &&
                !string.IsNullOrWhiteSpace(session))
            {
                credential = new TransportCredential
                {
                    Kind = TransportCredentialKind.Session,
                    Value = session,
                    TenantId = ctx.GetTenantContext().TenantId,
                    Device = ctx.GetDevice()
                };
                return true;
            }

            // Refresh token cookie
            if (ctx.Request.Cookies.TryGetValue("uauth.refresh", out var refresh) &&
                !string.IsNullOrWhiteSpace(refresh))
            {
                credential = new TransportCredential
                {
                    Kind = TransportCredentialKind.RefreshToken,
                    Value = refresh,
                    TenantId = ctx.GetTenantContext().TenantId,
                    Device = ctx.GetDevice()
                };
                return true;
            }

            return false;
        }

        private static bool TryFromQuery(
            HttpContext ctx,
            out TransportCredential credential)
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

        private static bool TryFromBody(
            HttpContext ctx,
            out TransportCredential credential)
        {
            credential = default!;
            // intentionally empty for now
            // body parsing is expensive and opt-in later
            return false;
        }

        private static bool TryFromHub(
            HttpContext ctx,
            out TransportCredential credential)
        {
            credential = default!;
            // UAuthHub detection can live here later
            return false;
        }
    }

}
