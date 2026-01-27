using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface ICredentialEndpointHandler
    {
        Task<IResult> GetAllAsync(HttpContext ctx);
        Task<IResult> AddAsync(HttpContext ctx);
        Task<IResult> ChangeAsync(string type, HttpContext ctx);
        Task<IResult> RevokeAsync(string type, HttpContext ctx);
        Task<IResult> ActivateAsync(string type, HttpContext ctx);
        Task<IResult> DeleteAsync(string type, HttpContext ctx);
    }
}
