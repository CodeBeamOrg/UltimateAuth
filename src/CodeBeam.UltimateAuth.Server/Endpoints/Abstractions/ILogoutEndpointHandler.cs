using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface ILogoutEndpointHandler
{
    Task<IResult> LogoutAsync(HttpContext ctx);
    Task<IResult> LogoutDeviceSelfAsync(HttpContext ctx);
    Task<IResult> LogoutOthersSelfAsync(HttpContext ctx);
    Task<IResult> LogoutAllSelfAsync(HttpContext ctx);

    Task<IResult> LogoutDeviceAdminAsync(HttpContext ctx, UserKey userKey);
    Task<IResult> LogoutOthersAdminAsync(HttpContext ctx, UserKey userKey);
    Task<IResult> LogoutAllAdminAsync(HttpContext ctx, UserKey userKey);
}
