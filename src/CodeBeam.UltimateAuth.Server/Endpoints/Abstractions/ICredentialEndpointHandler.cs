using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface ICredentialEndpointHandler
{
    Task<IResult> GetAllAsync(HttpContext ctx);
    Task<IResult> AddAsync(HttpContext ctx);
    Task<IResult> ChangeAsync(string type, HttpContext ctx);
    Task<IResult> RevokeAsync(string type, HttpContext ctx);
    Task<IResult> BeginResetAsync(string type, HttpContext ctx);
    Task<IResult> CompleteResetAsync(string type, HttpContext ctx);

    Task<IResult> GetAllAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> AddAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> RevokeAdminAsync(UserKey userKey, string type, HttpContext ctx);
    Task<IResult> ActivateAdminAsync(UserKey userKey, string type, HttpContext ctx);
    Task<IResult> DeleteAdminAsync(UserKey userKey, string type, HttpContext ctx);
    Task<IResult> BeginResetAdminAsync(UserKey userKey, string type, HttpContext ctx);
    Task<IResult> CompleteResetAdminAsync(UserKey userKey, string type, HttpContext ctx);
}
