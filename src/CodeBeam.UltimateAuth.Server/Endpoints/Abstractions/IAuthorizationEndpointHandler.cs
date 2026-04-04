using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface IAuthorizationEndpointHandler
{
    Task<IResult> CheckAsync(HttpContext ctx);
    Task<IResult> GetMyRolesAsync(HttpContext ctx);
    Task<IResult> GetUserRolesAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> AssignRoleAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> RemoveRoleAsync(UserKey userKey, HttpContext ctx);

    Task<IResult> CreateRoleAsync(HttpContext ctx);
    Task<IResult> RenameRoleAsync(RoleId roleId, HttpContext ctx);
    Task<IResult> DeleteRoleAsync(RoleId roleId, HttpContext ctx);
    Task<IResult> SetRolePermissionsAsync(RoleId roleId, HttpContext ctx);
    Task<IResult> QueryRolesAsync(HttpContext ctx);
}
