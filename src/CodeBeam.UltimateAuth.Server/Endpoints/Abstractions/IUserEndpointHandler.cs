using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface IUserEndpointHandler
{
    Task<IResult> CreateAsync(HttpContext ctx);
    Task<IResult> ChangeStatusSelfAsync(HttpContext ctx);
    Task<IResult> ChangeStatusAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> DeleteAsync(UserKey userKey, HttpContext ctx);

    Task<IResult> GetMeAsync(HttpContext ctx);
    Task<IResult> UpdateMeAsync(HttpContext ctx);

    Task<IResult> GetUserAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> UpdateUserAsync(UserKey userKey, HttpContext ctx);

    Task<IResult> GetMyIdentifiersAsync(HttpContext ctx);
    Task<IResult> IdentifierExistsSelfAsync(HttpContext ctx);
    Task<IResult> AddUserIdentifierSelfAsync(HttpContext ctx);
    Task<IResult> UpdateUserIdentifierSelfAsync(HttpContext ctx);
    Task<IResult> SetPrimaryUserIdentifierSelfAsync(HttpContext ctx);
    Task<IResult> UnsetPrimaryUserIdentifierSelfAsync(HttpContext ctx);
    Task<IResult> VerifyUserIdentifierSelfAsync(HttpContext ctx);
    Task<IResult> DeleteUserIdentifierSelfAsync(HttpContext ctx);

    Task<IResult> GetUserIdentifiersAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> IdentifierExistsAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> AddUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> UpdateUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> SetPrimaryUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> UnsetPrimaryUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> VerifyUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> DeleteUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx);
}
