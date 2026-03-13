using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class HttpContextTenantExtensions
{
    public static TenantKey GetTenant(this HttpContext context)
    {
        if (!context.Items.TryGetValue(UAuthConstants.HttpItems.TenantContextKey, out var value) || value is not UAuthTenantContext tenantCtx)
        {
            throw new InvalidOperationException("TenantContext is missing. TenantMiddleware must run before authentication.");
        }

        return tenantCtx.Tenant;
    }
}
