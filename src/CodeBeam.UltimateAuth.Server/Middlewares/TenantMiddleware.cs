using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Middlewares
{
    public sealed class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public const string TenantContextKey = "__UAuthTenant";

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantResolver resolver, IOptions<UAuthMultiTenantOptions> options)
        {
            var opts = options.Value;

            UAuthTenantContext tenantContext;

            if (!opts.Enabled)
            {
                tenantContext = UAuthTenantContext.NotResolved();
            }
            else
            {
                tenantContext = await resolver.ResolveAsync(context);

                if (opts.RequireTenant && !tenantContext.IsResolved)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Tenant is required but could not be resolved.");
                    return;
                }
            }

            context.Items[TenantContextKey] = tenantContext;

            await _next(context);
        }
    }
}
