using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class CookieSessionIdResolver : IInnerSessionIdResolver
    {
        private readonly UAuthSessionResolutionOptions _options;

        public CookieSessionIdResolver(IOptions<UAuthSessionResolutionOptions> options)
        {
            _options = options.Value;
        }

        public AuthSessionId? Resolve(HttpContext context)
        {
            if (!context.Request.Cookies.TryGetValue(_options.CookieName, out var raw))
                return null;

            return string.IsNullOrWhiteSpace(raw)
                ? null
                : new AuthSessionId(raw.Trim());
        }

    }
}
