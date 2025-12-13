using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Sessions
{
    public sealed class UAuthSessionIdResolver : ISessionIdResolver
    {
        private readonly ISessionIdResolver _inner;

        public UAuthSessionIdResolver(IOptions<UAuthSessionResolutionOptions> options)
        {
            var o = options.Value;

            var map = new Dictionary<string, ISessionIdResolver>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bearer"] = new BearerSessionIdResolver(),
                ["Header"] = new HeaderSessionIdResolver(o.HeaderName),
                ["Cookie"] = new CookieSessionIdResolver(o.CookieName),
                ["Query"] = new QuerySessionIdResolver(o.QueryParameterName),
            };

            var list = new List<ISessionIdResolver>();

            foreach (var key in o.Order)
            {
                if (!map.TryGetValue(key, out var resolver))
                    continue;

                if (key.Equals("Bearer", StringComparison.OrdinalIgnoreCase) && !o.EnableBearer) continue;
                if (key.Equals("Header", StringComparison.OrdinalIgnoreCase) && !o.EnableHeader) continue;
                if (key.Equals("Cookie", StringComparison.OrdinalIgnoreCase) && !o.EnableCookie) continue;
                if (key.Equals("Query", StringComparison.OrdinalIgnoreCase) && !o.EnableQuery) continue;

                list.Add(resolver);
            }

            _inner = new CompositeSessionIdResolver(list);
        }

        public AuthSessionId? Resolve(Microsoft.AspNetCore.Http.HttpContext context)
            => _inner.Resolve(context);
    }
}
