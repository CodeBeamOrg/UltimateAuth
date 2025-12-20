using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class CompositeSessionIdResolver : ISessionIdResolver
    {
        private readonly IReadOnlyList<IInnerSessionIdResolver> _resolvers;

        public CompositeSessionIdResolver(IEnumerable<IInnerSessionIdResolver> resolvers, IOptions<UAuthSessionResolutionOptions> options)
        {
            _resolvers = Order(resolvers, options.Value);
        }

        public AuthSessionId? Resolve(HttpContext context)
        {
            foreach (var resolver in _resolvers)
            {
                var id = resolver.Resolve(context);
                if (id is not null)
                    return id;
            }

            return null;
        }

        private static IReadOnlyList<IInnerSessionIdResolver> Order(IEnumerable<IInnerSessionIdResolver> resolvers, UAuthSessionResolutionOptions options)
        {
            var map = resolvers.ToDictionary(
                r => r.GetType().Name.Replace("SessionIdResolver", ""),
                r => r,
                StringComparer.OrdinalIgnoreCase);

            var ordered = new List<IInnerSessionIdResolver>();

            foreach (var key in options.Order)
            {
                if (map.TryGetValue(key, out var r))
                    ordered.Add(r);
            }

            return ordered;
        }

    }
}
