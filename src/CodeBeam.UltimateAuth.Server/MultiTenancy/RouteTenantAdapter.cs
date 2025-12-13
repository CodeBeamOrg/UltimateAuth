using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using Microsoft.AspNetCore.Http;

public sealed class RouteTenantAdapter : ITenantResolver
{
    private readonly ITenantIdResolver _coreResolver;
    private readonly UAuthMultiTenantOptions _options;

    public RouteTenantAdapter(
        PathTenantResolver coreResolver,
        UAuthMultiTenantOptions options)
    {
        _coreResolver = coreResolver;
        _options = options;
    }

    public async Task<UAuthTenantContext> ResolveAsync(HttpContext ctx)
    {
        if (!_options.Enabled)
            return UAuthTenantContext.NotResolved();

        var resolutionContext = TenantResolutionContextFactory.FromHttpContext(ctx);
        var tenantId = await _coreResolver.ResolveTenantIdAsync(resolutionContext);

        if (tenantId is null)
            return UAuthTenantContext.NotResolved();

        return UAuthTenantContextFactory.Create(tenantId, _options);
    }
}
