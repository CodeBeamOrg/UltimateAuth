using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Users
{
    public interface IUserAccessor
    {
        Task ResolveAsync(HttpContext context);
    }
}
