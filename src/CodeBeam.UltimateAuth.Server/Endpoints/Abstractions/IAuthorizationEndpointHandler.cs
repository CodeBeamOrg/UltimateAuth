using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IAuthorizationEndpointHandler
    {
        Task<IResult> CheckAsync(HttpContext ctx);
        Task<IResult> GetRolesAsync(UserKey userKey, HttpContext ctx);
        Task<IResult> AssignRoleAsync(UserKey userKey, HttpContext ctx);
        Task<IResult> RemoveRoleAsync(UserKey userKey, HttpContext ctx);
    }
}
