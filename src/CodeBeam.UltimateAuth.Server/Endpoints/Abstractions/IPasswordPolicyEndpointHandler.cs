using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface IPasswordPolicyEndpointHandler
{
    IResult GetAsync(HttpContext context);
}
