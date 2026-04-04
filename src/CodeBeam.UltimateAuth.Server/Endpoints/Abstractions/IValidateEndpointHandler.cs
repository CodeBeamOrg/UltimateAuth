using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface IValidateEndpointHandler
{
    Task<IResult> ValidateAsync(HttpContext context, CancellationToken ct = default);
}
