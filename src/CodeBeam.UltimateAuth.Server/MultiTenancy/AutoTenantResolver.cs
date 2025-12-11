using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    public class AutoTenantResolver : ITenantResolver
    {
        private readonly ITenantResolver _routeResolver;
        private readonly ITenantResolver _headerResolver;
        private readonly ITenantResolver _domainResolver;
        private readonly UAuthMultiTenantOptions _options;

        public AutoTenantResolver(RouteTenantResolver routeResolver, HeaderTenantResolver headerResolver, DomainTenantResolver domainResolver, UAuthMultiTenantOptions options)
        {
            _routeResolver = routeResolver;
            _headerResolver = headerResolver;
            _domainResolver = domainResolver;
            _options = options;
        }

        public async Task<UAuthTenantContext> ResolveAsync(HttpContext ctx)
        {
            var routeCtx = await _routeResolver.ResolveAsync(ctx);
            if (routeCtx.IsResolved)
                return Normalize(routeCtx);

            var headerCtx = await _headerResolver.ResolveAsync(ctx);
            if (headerCtx.IsResolved)
                return Normalize(headerCtx);

            var domainCtx = await _domainResolver.ResolveAsync(ctx);
            if (domainCtx.IsResolved)
                return Normalize(domainCtx);

            if (_options.RequireTenant)
            {
                return new UAuthTenantContext
                {
                    TenantId = null,
                    IsResolved = false
                };
            }

            return new UAuthTenantContext
            {
                TenantId = _options.DefaultTenantId,
                IsResolved = true
            };
        }

        private UAuthTenantContext Normalize(UAuthTenantContext ctx)
        {
            if (_options.NormalizeToLowercase && ctx.TenantId != null)
                ctx.TenantId = ctx.TenantId.ToLowerInvariant();

            return ctx;
        }
    }
}
