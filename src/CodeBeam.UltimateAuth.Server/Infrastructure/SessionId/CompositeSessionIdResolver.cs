using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

// TODO: Add policy and effective auth resolver.
public sealed class CompositeSessionIdResolver : ISessionIdResolver
{
    private readonly IReadOnlyDictionary<string, IInnerSessionIdResolver> _resolvers;
    private readonly UAuthSessionResolutionOptions _options;

    public CompositeSessionIdResolver(IEnumerable<IInnerSessionIdResolver> resolvers, IOptions<UAuthServerOptions> options)
    {
        _options = options.Value.SessionResolution;
        _resolvers = resolvers.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
    }

    public AuthSessionId? Resolve(HttpContext context)
    {
        foreach (var name in _options.Order)
        {
            if (!IsEnabled(name))
                continue;

            if (!_resolvers.TryGetValue(name, out var resolver))
                continue;

            var id = resolver.Resolve(context);
            if (id is not null)
                return id;
        }

        return null;
    }

    private bool IsEnabled(string name)
    {
        if (name.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            return _options.EnableBearer;

        if (name.Equals("Header", StringComparison.OrdinalIgnoreCase))
            return _options.EnableHeader;

        if (name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            return _options.EnableCookie;

        if (name.Equals("Query", StringComparison.OrdinalIgnoreCase))
            return _options.EnableQuery;

        return false;
    }
}
