using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IUserAccessor
    {
        Task ResolveAsync(HttpContext context);
    }
}
