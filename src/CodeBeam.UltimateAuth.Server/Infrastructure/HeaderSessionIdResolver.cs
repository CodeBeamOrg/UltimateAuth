using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class HeaderSessionIdResolver : IInnerSessionIdResolver
    {
        private readonly UAuthSessionResolutionOptions _options;

        public HeaderSessionIdResolver(IOptions<UAuthSessionResolutionOptions> options)
        {
            _options = options.Value;
        }

        public AuthSessionId? Resolve(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var values))
                return null;

            var raw = values.FirstOrDefault();
            return string.IsNullOrWhiteSpace(raw) ? null : new AuthSessionId(raw);
        }
    }
}
