using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface ISessionRefreshEndpointHandler
    {
        Task<IResult> RefreshSessionAsync(HttpContext ctx);
    }
}
