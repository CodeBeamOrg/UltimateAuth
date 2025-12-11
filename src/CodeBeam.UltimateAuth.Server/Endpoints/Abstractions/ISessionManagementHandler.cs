using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface ISessionManagementHandler
    {
        Task<IResult> GetCurrentSessionAsync(HttpContext ctx);
        Task<IResult> GetAllSessionsAsync(HttpContext ctx);
        Task<IResult> RevokeSessionAsync(string sessionId, HttpContext ctx);
        Task<IResult> RevokeAllAsync(HttpContext ctx);
    }
}
