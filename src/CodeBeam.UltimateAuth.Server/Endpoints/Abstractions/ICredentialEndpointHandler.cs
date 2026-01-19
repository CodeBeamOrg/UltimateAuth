using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface ICredentialEndpointHandler
    {
        Task<IResult> SetInitialAsync(HttpContext ctx);
        Task<IResult> ResetPasswordAsync(HttpContext ctx);
        Task<IResult> RevokeAllAsync(HttpContext ctx);
    }
}
