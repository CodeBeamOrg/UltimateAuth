using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class CookieSessionIdResolver : IInnerSessionIdResolver
    {
        private readonly UAuthServerOptions _options;

        public CookieSessionIdResolver(IOptions<UAuthServerOptions> options)
        {
            _options = options.Value;
        }

        public AuthSessionId? Resolve(HttpContext context)
        {
            if (!context.Request.Cookies.TryGetValue(_options.Cookie.Name, out var raw))
                return null;

            return string.IsNullOrWhiteSpace(raw)
                ? null
                : new AuthSessionId(raw.Trim());
        }

    }
}
