using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    internal sealed class RefreshEndpointHandlerBridge : IRefreshEndpointHandler
    {
        private readonly DefaultRefreshEndpointHandler<UserId> _inner;

        public RefreshEndpointHandlerBridge(
            DefaultRefreshEndpointHandler<UserId> inner)
        {
            _inner = inner;
        }

        public Task<IResult> RefreshAsync(HttpContext ctx)
            => _inner.RefreshAsync(ctx);
    }
}
