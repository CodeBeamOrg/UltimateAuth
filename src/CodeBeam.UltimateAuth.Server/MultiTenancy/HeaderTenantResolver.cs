using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    public class HeaderTenantResolver : ITenantResolver
    {
        private readonly UAuthMultiTenantOptions _options;
        private const string HeaderName = "X-Tenant";

        public HeaderTenantResolver(UAuthMultiTenantOptions options)
        {
            _options = options;
        }

        public Task<UAuthTenantContext> ResolveAsync(HttpContext ctx)
        {
            if (!_options.Enabled)
                return Task.FromResult(NotResolved());

            if (!ctx.Request.Headers.TryGetValue(HeaderName, out var values))
                return Task.FromResult(NotResolved());

            var tenantId = values.ToString();
            if (string.IsNullOrWhiteSpace(tenantId))
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
