using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface IRefreshEndpointHandler
{
    Task<IResult> RefreshAsync(HttpContext ctx);
}
