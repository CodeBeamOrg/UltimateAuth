using CodeBeam.UltimateAuth.Core.Constants;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestHttpContext
{
    public static HttpContext Create(TenantKey? tenant = null, UAuthClientProfile clientProfile = UAuthClientProfile.NotSpecified)
    {
        var ctx = new DefaultHttpContext();
        
        var resolvedTenant = tenant ?? TenantKey.Single;
        ctx.Items[UAuthConstants.HttpItems.TenantContextKey] = UAuthTenantContext.Resolved(resolvedTenant);
        
        ctx.Request.Headers["User-Agent"] = "UltimateAuth-Test";
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("app.example.com");

        return ctx;
    }
}
