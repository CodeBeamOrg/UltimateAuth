using System.Text.RegularExpressions;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    internal static class TenantValidation
    {
        public static UAuthTenantContext FromResolvedTenant(
            string rawTenantId,
            UAuthMultiTenantOptions options)
        {
            if (string.IsNullOrWhiteSpace(rawTenantId))
                return UAuthTenantContext.NotResolved();

            var tenantId = options.NormalizeToLowercase
                ? rawTenantId.ToLowerInvariant()
                : rawTenantId;

            if (!Regex.IsMatch(tenantId, options.TenantIdRegex))
                return UAuthTenantContext.NotResolved();

            if (options.ReservedTenantIds.Contains(tenantId))
                return UAuthTenantContext.NotResolved();

            return UAuthTenantContext.Resolved(tenantId);
        }
    }
}
