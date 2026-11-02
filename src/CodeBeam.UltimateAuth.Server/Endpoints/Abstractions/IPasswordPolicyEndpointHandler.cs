using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface IPasswordPolicyEndpointHandler
{
    Task<IResult> GetAsync(HttpContext context);
}
