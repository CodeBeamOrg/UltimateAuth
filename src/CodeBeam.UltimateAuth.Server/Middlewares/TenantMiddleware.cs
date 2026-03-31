using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Middlewares;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

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
            context.Items[UAuthConstants.HttpItems.TenantContextKey] = UAuthTenantContext.SingleTenant();
            await _next(context);
            return;
        }

        resolution = await resolver.ResolveAsync(context);

        // Middleware must allow unresolved tenants for non-auth requests.
        // Exception should be handled only in AuthFlowContextFactory, where we can check if the request is for auth endpoints or not.
        if (!resolution.IsResolved)
        {
            context.Items[UAuthConstants.HttpItems.TenantContextKey] = UAuthTenantContext.Unresolved();
            await _next(context);
            return;
        }

        var tenantContext = UAuthTenantContext.Resolved(resolution.Tenant);

        context.Items[UAuthConstants.HttpItems.TenantContextKey] = tenantContext;
        await _next(context);
    }
}
