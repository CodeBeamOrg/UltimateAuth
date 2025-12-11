using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface ILoginEndpointHandler
    {
        Task<IResult> LoginAsync(HttpContext ctx);
    }
}
