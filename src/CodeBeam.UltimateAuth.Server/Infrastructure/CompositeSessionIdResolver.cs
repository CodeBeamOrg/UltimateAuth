using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class CompositeSessionIdResolver : ISessionIdResolver
    {
        private readonly IReadOnlyList<ISessionIdResolver> _resolvers;

        public CompositeSessionIdResolver(IEnumerable<ISessionIdResolver> resolvers)
        {
            _resolvers = resolvers.ToList();
        }

        public AuthSessionId? Resolve(HttpContext context)
        {
            foreach (var r in _resolvers)
            {
                var id = r.Resolve(context);
                if (id is not null)
                    return id;
            }

            return null;
        }
    }
}
