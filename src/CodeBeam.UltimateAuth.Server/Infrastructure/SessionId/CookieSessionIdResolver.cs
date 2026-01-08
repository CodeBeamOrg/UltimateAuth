using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class CookieSessionIdResolver : IInnerSessionIdResolver
    {
        public string Key => "cookie";

        private readonly UAuthServerOptions _options;

        public CookieSessionIdResolver(IOptions<UAuthServerOptions> options)
        {
            _options = options.Value;
        }

        public AuthSessionId? Resolve(HttpContext context)
        {
            var cookieName = _options.Cookie.Session.Name;

            if (!context.Request.Cookies.TryGetValue(cookieName, out var raw))
                return null;

            return string.IsNullOrWhiteSpace(raw)
                ? null
                : new AuthSessionId(raw.Trim());
        }
    }
}
