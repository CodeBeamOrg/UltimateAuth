using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    public sealed class HeaderTenantAdapter : ITenantResolver
    {
        private readonly ITenantIdResolver _coreResolver;
        private readonly UAuthMultiTenantOptions _options;

        public HeaderTenantAdapter(
            HeaderTenantResolver coreResolver,
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
}
