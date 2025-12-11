using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Middlewares
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITenantResolver _resolver;
        private readonly UAuthMultiTenantOptions _options;

        public TenantMiddleware(RequestDelegate next, ITenantResolver resolver, UAuthMultiTenantOptions options)
        {
            _next = next;
            _resolver = resolver;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            if (!_options.Enabled)
            {
                await _next(ctx);
                return;
            }

            var tenantCtx = await _resolver.ResolveAsync(ctx);

            if (_options.RequireTenant && !tenantCtx.IsResolved)
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsync("Tenant is required but could not be resolved.");
                return;
            }

            ctx.Items["Tenant"] = tenantCtx.TenantId;

            await _next(ctx);
        }
    }
}
