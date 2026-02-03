using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Middlewares;

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
        TenantResolutionResult resolution;

        if (!opts.Enabled)
        {
            context.Items[TenantContextKey] = UAuthTenantContext.SingleTenant();
            await _next(context);
            return;
        }

        resolution = await resolver.ResolveAsync(context);

        if (!resolution.IsResolved)
        {
            if (opts.RequireTenant)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Tenant is required.");
                return;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Tenant could not be resolved.");
            return;
        }

        var tenantContext = UAuthTenantContext.Resolved(resolution.Tenant);

        context.Items[TenantContextKey] = tenantContext;
        await _next(context);
    }
}
