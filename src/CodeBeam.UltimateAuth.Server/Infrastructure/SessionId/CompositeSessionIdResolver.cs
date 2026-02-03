using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

// TODO: Add policy and effective auth resolver.
public sealed class CompositeSessionIdResolver : ISessionIdResolver
{
    private readonly IReadOnlyList<IInnerSessionIdResolver> _resolvers;

    public CompositeSessionIdResolver(IEnumerable<IInnerSessionIdResolver> resolvers)
    {
        _resolvers = resolvers.ToList();
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
}
