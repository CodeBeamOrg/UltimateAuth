using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    /// <summary>
    /// Server-level tenant resolver.
    /// Responsible for executing core tenant id resolvers and
    /// applying UltimateAuth tenant policies.
    /// </summary>
    public sealed class UAuthTenantResolver : ITenantResolver
    {
        private readonly ITenantIdResolver _idResolver;
        private readonly UAuthMultiTenantOptions _options;

        public UAuthTenantResolver(
            ITenantIdResolver idResolver,
            UAuthMultiTenantOptions options)
        {
            _idResolver = idResolver;
            _options = options;
        }

        public async Task<UAuthTenantContext> ResolveAsync(HttpContext context)
        {
            if (!_options.Enabled)
                return UAuthTenantContext.NotResolved();

            var resolutionContext =
                TenantResolutionContextFactory.FromHttpContext(context);

            var rawTenantId =
                await _idResolver.ResolveTenantIdAsync(resolutionContext);

            if (string.IsNullOrWhiteSpace(rawTenantId))
            {
                if (_options.RequireTenant)
                    return UAuthTenantContext.NotResolved();

                if (_options.DefaultTenantId is null)
                    return UAuthTenantContext.NotResolved();

                return UAuthTenantContext.Resolved(
                    Normalize(_options.DefaultTenantId));
            }

            var tenantId = Normalize(rawTenantId);

            if (!IsValid(tenantId))
                return UAuthTenantContext.NotResolved();

            return UAuthTenantContext.Resolved(tenantId);
        }

        private string Normalize(string tenantId)
        {
            return _options.NormalizeToLowercase
                ? tenantId.ToLowerInvariant()
                : tenantId;
        }

        private bool IsValid(string tenantId)
        {
            if (!System.Text.RegularExpressions.Regex
                .IsMatch(tenantId, _options.TenantIdRegex))
                return false;

            if (_options.ReservedTenantIds.Contains(tenantId))
                return false;

            return true;
        }
    }
}
