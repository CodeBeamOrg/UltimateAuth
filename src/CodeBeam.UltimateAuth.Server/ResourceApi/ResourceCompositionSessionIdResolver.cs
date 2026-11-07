using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class ResourceCompositeSessionIdResolver : ISessionIdResolver
{
    private readonly IReadOnlyDictionary<string, IInnerSessionIdResolver> _resolvers;
    private readonly UAuthSessionResolutionOptions _options;

    public ResourceCompositeSessionIdResolver(IEnumerable<IInnerSessionIdResolver> resolvers, IOptions<UAuthServerOptions> options)
    {
        _options = options.Value.SessionResolution;
        _resolvers = resolvers.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<AuthSessionId?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        foreach (var name in _options.Order)
        {
            if (!IsEnabled(name))
                continue;

            if (!_resolvers.TryGetValue(name, out var resolver))
                continue;

            var id = resolver.Resolve(context);

            if (id is not null)
                return ValueTask.FromResult<AuthSessionId?>(id);
        }

        return ValueTask.FromResult<AuthSessionId?>(null);
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
