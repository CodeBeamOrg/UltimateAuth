using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IUserInfoEndpointHandler
    {
        Task<IResult> GetUserInfoAsync(HttpContext ctx);
        Task<IResult> GetPermissionsAsync(HttpContext ctx);
        Task<IResult> CheckPermissionAsync(HttpContext ctx);
    }
}
