using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface ICredentialEndpointHandler
{
    Task<IResult> GetAllAsync(HttpContext ctx);
    Task<IResult> AddAsync(HttpContext ctx);
    Task<IResult> ChangeSecretAsync(HttpContext ctx);
    Task<IResult> RevokeAsync(HttpContext ctx);
    Task<IResult> BeginResetAsync(HttpContext ctx);
    Task<IResult> CompleteResetAsync(HttpContext ctx);

    Task<IResult> GetAllAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> AddAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> RevokeAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> DeleteAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> BeginResetAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> CompleteResetAdminAsync(UserKey userKey, HttpContext ctx);
}
