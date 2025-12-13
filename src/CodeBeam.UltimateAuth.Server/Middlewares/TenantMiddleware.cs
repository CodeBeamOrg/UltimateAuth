using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Middlewares
{
    public sealed class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITenantResolver _resolver;
        private readonly UAuthMultiTenantOptions _options;

        public const string TenantContextKey = "__UAuthTenant";

        public TenantMiddleware(
            RequestDelegate next,
            ITenantResolver resolver,
            UAuthMultiTenantOptions options)
        {
            _next = next;
            _resolver = resolver;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            UAuthTenantContext tenantContext;

            if (!_options.Enabled)
            {
                // Single-tenant mode → tenant concept disabled
                tenantContext = UAuthTenantContext.NotResolved();
            }
            else
            {
                tenantContext = await _resolver.ResolveAsync(context);

                if (_options.RequireTenant && !tenantContext.IsResolved)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(
                        "Tenant is required but could not be resolved.");
                    return;
                }
            }

            context.Items[TenantContextKey] = tenantContext;

            await _next(context);
        }
    }
}
