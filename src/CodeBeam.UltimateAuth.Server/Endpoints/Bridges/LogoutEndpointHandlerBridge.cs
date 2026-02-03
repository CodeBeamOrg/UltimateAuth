using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class LogoutEndpointHandlerBridge : ILogoutEndpointHandler
{
    private readonly LogoutEndpointHandler<UserKey> _inner;

    public LogoutEndpointHandlerBridge(LogoutEndpointHandler<UserKey> inner)
    {
        _inner = inner;
    }

    public Task<IResult> LogoutAsync(HttpContext ctx)
        => _inner.LogoutAsync(ctx);
}
