using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using System.Text.RegularExpressions;

public static class UAuthTenantContextFactory
{
    public static UAuthTenantContext Create(
        string tenantId,
        UAuthMultiTenantOptions options)
    {
        if (options.NormalizeToLowercase)
            tenantId = tenantId.ToLowerInvariant();

        if (!Regex.IsMatch(tenantId, options.TenantIdRegex))
            return UAuthTenantContext.NotResolved();

        if (options.ReservedTenantIds.Contains(tenantId))
            return UAuthTenantContext.NotResolved();

        return UAuthTenantContext.Resolved(tenantId);
    }
}
