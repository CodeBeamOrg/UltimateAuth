using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface ILogoutEndpointHandler
    {
        Task<IResult> LogoutAsync(HttpContext ctx);
    }
}
