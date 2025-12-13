using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Sessions
{
    public sealed class HeaderSessionIdResolver : ISessionIdResolver
    {
        private readonly string _headerName;

        public HeaderSessionIdResolver(string headerName)
        {
            _headerName = headerName;
        }

        public AuthSessionId? Resolve(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(_headerName, out var values))
                return null;

            var raw = values.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return new AuthSessionId(raw.Trim());
        }
    }
}
