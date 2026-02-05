using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Middlewares;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestHttpContext
{
    public static HttpContext Create(TenantKey? tenant = null, UAuthClientProfile clientProfile = UAuthClientProfile.NotSpecified)
    {
        var ctx = new DefaultHttpContext();
        
        var resolvedTenant = tenant ?? TenantKey.Single;
        ctx.Items[TenantMiddleware.TenantContextKey] = UAuthTenantContext.Resolved(resolvedTenant);
        
        ctx.Request.Headers["User-Agent"] = "UltimateAuth-Test";

        return ctx;
    }
}
