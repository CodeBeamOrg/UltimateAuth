using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class ValidateEndpointHandlerBridge : IValidateEndpointHandler
{
    private readonly ValidateEndpointHandler _inner;

    public ValidateEndpointHandlerBridge(ValidateEndpointHandler inner)
    {
        _inner = inner;
    }

    public Task<IResult> ValidateAsync(HttpContext context, CancellationToken ct = default) => _inner.ValidateAsync(context, ct);
}
