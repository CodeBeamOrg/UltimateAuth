using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IReauthEndpointHandler
    {
        Task<IResult> ReauthAsync(HttpContext ctx);
    }
}
