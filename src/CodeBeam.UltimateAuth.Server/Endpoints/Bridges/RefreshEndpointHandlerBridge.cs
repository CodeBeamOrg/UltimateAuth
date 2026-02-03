using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class RefreshEndpointHandlerBridge : IRefreshEndpointHandler
{
    private readonly RefreshEndpointHandler _inner;

    public RefreshEndpointHandlerBridge(RefreshEndpointHandler inner)
    {
        _inner = inner;
    }

    public Task<IResult> RefreshAsync(HttpContext ctx) => _inner.RefreshAsync(ctx);
}
