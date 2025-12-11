using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IPkceEndpointHandler
    {
        Task<IResult> CreateAsync(HttpContext ctx);
        Task<IResult> VerifyAsync(HttpContext ctx);
        Task<IResult> ConsumeAsync(HttpContext ctx);
    }
}
