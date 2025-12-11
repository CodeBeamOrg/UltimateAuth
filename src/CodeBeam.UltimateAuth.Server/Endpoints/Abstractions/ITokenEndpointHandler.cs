using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface ITokenEndpointHandler
    {
        Task<IResult> GetTokenAsync(HttpContext ctx);
        Task<IResult> RefreshTokenAsync(HttpContext ctx);
        Task<IResult> IntrospectAsync(HttpContext ctx);
        Task<IResult> RevokeAsync(HttpContext ctx);
    }
}
