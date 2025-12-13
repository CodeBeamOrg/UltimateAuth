using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    public interface ITenantResolver
    {
        Task<UAuthTenantContext> ResolveAsync(HttpContext ctx);
    }
}

