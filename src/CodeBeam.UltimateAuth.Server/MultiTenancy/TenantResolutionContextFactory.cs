using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    public static class TenantResolutionContextFactory
    {
        public static TenantResolutionContext FromHttpContext(HttpContext ctx)
        {
            var headers = ctx.Request.Headers
                .ToDictionary(
                    h => h.Key,
                    h => h.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);

            var query = ctx.Request.Query
                .ToDictionary(
                    q => q.Key,
                    q => q.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);

            return TenantResolutionContext.Create(
                headers: headers,
                Query: query,
                host: ctx.Request.Host.Host,
                path: ctx.Request.Path.Value,
                rawContext: ctx
            );
        }
    }
}
