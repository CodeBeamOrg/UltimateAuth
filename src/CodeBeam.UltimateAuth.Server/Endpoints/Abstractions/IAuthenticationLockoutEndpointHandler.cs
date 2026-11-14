using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface IAuthenticationLockoutEndpointHandler
{
    Task<IResult> GetAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> UnlockAsync(UserKey userKey, HttpContext ctx);
}
