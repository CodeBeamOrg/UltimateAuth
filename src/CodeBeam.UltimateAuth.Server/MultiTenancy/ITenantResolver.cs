using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    public interface ITenantResolver
    {
        Task<UAuthTenantContext> ResolveAsync(HttpContext ctx);
    }
}

