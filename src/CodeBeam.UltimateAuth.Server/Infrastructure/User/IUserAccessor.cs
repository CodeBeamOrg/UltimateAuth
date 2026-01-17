using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IUserAccessor<TUserId>
    {
        Task ResolveAsync(HttpContext context);
    }

    public interface IUserAccessor
    {
        Task ResolveAsync(HttpContext context);
    }
}
