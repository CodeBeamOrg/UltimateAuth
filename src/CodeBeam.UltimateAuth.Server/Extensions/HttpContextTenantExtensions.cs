using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class HttpContextTenantExtensions
    {
        public static string? GetTenantId(this HttpContext ctx)
            => ctx.Items.TryGetValue("Tenant", out var v) ? v?.ToString() : null;
    }
}
