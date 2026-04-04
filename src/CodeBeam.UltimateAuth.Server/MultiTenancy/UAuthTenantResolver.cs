using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy;

public sealed class UAuthTenantResolver : ITenantResolver
{
    private readonly ITenantIdResolver _idResolver;
    private readonly UAuthMultiTenantOptions _options;

    public UAuthTenantResolver(ITenantIdResolver idResolver, IOptions<UAuthMultiTenantOptions> options)
    {
        _idResolver = idResolver;
        _options = options.Value;
    }

    public async Task<TenantResolutionResult> ResolveAsync(HttpContext context)
    {
        var resolutionContext =TenantResolutionContextFactory.FromHttpContext(context);

        var raw = await _idResolver.ResolveTenantIdAsync(resolutionContext);

        if (string.IsNullOrWhiteSpace(raw))
            return TenantResolutionResult.NotResolved();

        var normalized = _options.NormalizeToLowercase
            ? raw.Trim().ToLowerInvariant()
            : raw.Trim();

        if (!TenantKey.TryParse(normalized, null, out var tenant))
            return TenantResolutionResult.NotResolved();

        return TenantResolutionResult.Resolved(tenant);
    }
}
