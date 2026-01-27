using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IUserProfileAdminEndpointHandler
    {
        Task<IResult> GetAsync(UserKey userKey, HttpContext ctx);
        Task<IResult> UpdateAsync(UserKey userKey, HttpContext ctx);
    }
}
