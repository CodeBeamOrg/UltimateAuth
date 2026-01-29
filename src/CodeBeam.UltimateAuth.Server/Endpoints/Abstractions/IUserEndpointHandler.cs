using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface IUserEndpointHandler
{
    Task<IResult> CreateAsync(HttpContext ctx);
    Task<IResult> ChangeStatusSelfAsync(HttpContext ctx);
    Task<IResult> ChangeStatusAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> DeleteAsync(HttpContext ctx);

    Task<IResult> GetMeAsync(HttpContext ctx);
    Task<IResult> UpdateMeAsync(HttpContext ctx);

    Task<IResult> GetUserAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> UpdateUserAsync(UserKey userKey, HttpContext ctx);
}
