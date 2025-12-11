using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    public class DomainTenantResolver : ITenantResolver
    {
        private readonly UAuthMultiTenantOptions _options;

        public DomainTenantResolver(UAuthMultiTenantOptions options)
        {
            _options = options;
        }

        public Task<UAuthTenantContext> ResolveAsync(HttpContext ctx)
        {
            if (!_options.Enabled)
                return Task.FromResult(NotResolved());

            var host = ctx.Request.Host.Host; // e.g. contoso.myapp.com
            if (string.IsNullOrWhiteSpace(host))
                return Task.FromResult(NotResolved());

            var parts = host.Split('.');
            if (parts.Length < 3)
                return Task.FromResult(NotResolved()); // No subdomain → no tenant

            string tenantId = parts[0];

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
