using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IUserLifecycleEndpointHandler
    {
        Task<IResult> CreateAsync(HttpContext ctx);
        Task<IResult> ChangeStatusAsync(HttpContext ctx);
        Task<IResult> DeleteAsync(HttpContext ctx);
    }
}
