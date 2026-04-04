using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface ILoginEndpointHandler
{
    Task<IResult> LoginAsync(HttpContext ctx);
    Task<IResult> TryLoginAsync(HttpContext ctx);
}
