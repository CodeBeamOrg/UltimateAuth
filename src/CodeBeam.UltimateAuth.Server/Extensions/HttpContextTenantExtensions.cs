using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Middlewares;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class HttpContextTenantExtensions
    {
        public static string? GetTenantId(this HttpContext ctx)
        {
            return ctx.GetTenantContext().TenantId;
        }

        public static UAuthTenantContext GetTenantContext(this HttpContext ctx)
        {
            if (ctx.Items.TryGetValue(
                TenantMiddleware.TenantContextKey,
                out var value)
                && value is UAuthTenantContext tenant)
            {
                return tenant;
            }

            return UAuthTenantContext.NotResolved();
        }
    }
}
