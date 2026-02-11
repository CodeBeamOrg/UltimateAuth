using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class ClientBaseAddressResolver
{
    private readonly IReadOnlyList<IClientBaseAddressProvider> _providers;

    public ClientBaseAddressResolver(IEnumerable<IClientBaseAddressProvider> providers)
    {
        _providers = providers.ToList();
    }

    public string Resolve(HttpContext ctx, UAuthServerOptions options)
    {
        string? fallback = null;

        foreach (var provider in _providers)
        {
            if (!provider.TryResolve(ctx, options, out var candidate))
                continue;

            if (provider is IFallbackClientBaseAddressProvider)
            {
                fallback ??= candidate;
                continue;
            }

            return Validate(candidate, options);
        }

        if (fallback is not null)
            return Validate(fallback, options);

        throw new InvalidOperationException("Unable to resolve client base address from request.");
    }

    private static string Validate(string baseAddress, UAuthServerOptions options)
    {
        if (options.Hub.AllowedClientOrigins.Count == 0)
            return baseAddress;

        if (options.Hub.AllowedClientOrigins.Any(o => Normalize(o) == Normalize(baseAddress)))
            return baseAddress;

        throw new InvalidOperationException($"Redirect to '{baseAddress}' is not allowed. " +
            "The origin is not present in AllowedClientOrigins.");
    }

    private static string Normalize(string uri) => uri.TrimEnd('/').ToLowerInvariant();
}

