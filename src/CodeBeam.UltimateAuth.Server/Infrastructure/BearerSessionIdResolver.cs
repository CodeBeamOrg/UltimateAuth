using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class BearerSessionIdResolver : IInnerSessionIdResolver
    {
        public AuthSessionId? Resolve(HttpContext context)
        {
            var header = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(header))
                return null;

            if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            var raw = header["Bearer ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return new AuthSessionId(raw);
        }
    }
}
