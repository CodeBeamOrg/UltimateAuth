using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy;

public static class UAuthTenantContextFactory
{
    public static UAuthTenantContext Create(string? rawTenantId, UAuthMultiTenantOptions options)
    {
        // 🔹 Single-tenant mode
        if (!options.Enabled)
            return UAuthTenantContext.SingleTenant();

        if (string.IsNullOrWhiteSpace(rawTenantId))
        {
            if (options.RequireTenant)
                throw new InvalidOperationException("Tenant is required but could not be resolved.");

            throw new InvalidOperationException("Tenant could not be resolved.");
        }

        var tenantId = options.NormalizeToLowercase
            ? rawTenantId.Trim().ToLowerInvariant()
            : rawTenantId.Trim();

        var tenantKey = TenantKey.FromExternal(tenantId);
        return UAuthTenantContext.Resolved(tenantKey);
    }
}
