using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    public class RouteTenantResolver : ITenantResolver
    {
        private readonly UAuthMultiTenantOptions _options;

        public RouteTenantResolver(UAuthMultiTenantOptions options)
        {
            _options = options;
        }

        public Task<UAuthTenantContext> ResolveAsync(HttpContext ctx)
        {
            if (!_options.Enabled)
                return Task.FromResult(NotResolved());

            if (!ctx.Request.RouteValues.TryGetValue("tenant", out var tenantObj))
                return Task.FromResult(NotResolved());

            string? tenantId = tenantObj?.ToString();
            if (tenantId is null)
                return Task.FromResult(NotResolved());

            if (_options.NormalizeToLowercase)
                tenantId = tenantId.ToLowerInvariant();

            if (!System.Text.RegularExpressions.Regex.IsMatch(tenantId, _options.TenantIdRegex))
                return Task.FromResult(NotResolved());

            if (_options.ReservedTenantIds.Contains(tenantId))
                return Task.FromResult(NotResolved());

            return Task.FromResult(Resolved(tenantId));
        }

        private static UAuthTenantContext NotResolved() => new()
        {
            TenantId = null,
            IsResolved = false
        };

        private static UAuthTenantContext Resolved(string tenant) => new()
        {
            TenantId = tenant,
            IsResolved = true
        };

    }
}
