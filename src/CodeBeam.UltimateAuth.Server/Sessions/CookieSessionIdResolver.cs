using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Sessions
{
    public sealed class CookieSessionIdResolver : ISessionIdResolver
    {
        private readonly string _cookieName;

        public CookieSessionIdResolver(string cookieName)
        {
            _cookieName = cookieName;
        }

        public AuthSessionId? Resolve(HttpContext context)
        {
            if (!context.Request.Cookies.TryGetValue(_cookieName, out var raw))
                return null;

            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return new AuthSessionId(raw.Trim());
        }
    }
}
