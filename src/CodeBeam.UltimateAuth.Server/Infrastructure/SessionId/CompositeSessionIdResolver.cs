using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

// TODO: Add policy and effective auth resolver.
public sealed class CompositeSessionIdResolver : ISessionIdResolver
{
    private readonly IReadOnlyDictionary<string, IInnerSessionIdResolver> _resolvers;
    private readonly IClientProfileReader _clientProfileReader;
    private readonly IEffectiveServerOptionsProvider _effectiveOptionsProvider;

    public CompositeSessionIdResolver(IEnumerable<IInnerSessionIdResolver> resolvers, IClientProfileReader clientProfileReader, IEffectiveServerOptionsProvider effectiveOptionsProvider)
    {
        _resolvers = resolvers.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        _clientProfileReader = clientProfileReader;
        _effectiveOptionsProvider = effectiveOptionsProvider;
    }

    public async ValueTask<AuthSessionId?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var clientProfile = await _clientProfileReader.ReadAsync(context);
        var effective = _effectiveOptionsProvider.GetEffective(context, clientProfile);
        var options = effective.Options.SessionResolution;

        foreach (var name in options.Order)
        {
            if (!IsEnabled(name, options))
                continue;

            if (!_resolvers.TryGetValue(name, out var resolver))
                continue;

            var id = resolver.Resolve(context);

            if (id is not null)
                return id;
        }

        return null;
    }

    private static bool IsEnabled(string name, UAuthSessionResolutionOptions options)
    {
        if (name.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            return options.EnableBearer;

        if (name.Equals("Header", StringComparison.OrdinalIgnoreCase))
            return options.EnableHeader;

        if (name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            return options.EnableCookie;

        if (name.Equals("Query", StringComparison.OrdinalIgnoreCase))
            return options.EnableQuery;

        return false;
    }
}
