using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IUserProfileEndpointHandler
    {
        Task<IResult> GetAsync(HttpContext ctx);
        Task<IResult> UpdateAsync(HttpContext ctx);
    }
}
