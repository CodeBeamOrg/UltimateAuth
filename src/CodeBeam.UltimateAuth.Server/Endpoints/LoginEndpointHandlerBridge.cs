using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    internal sealed class LoginEndpointHandlerBridge : ILoginEndpointHandler
    {
        private readonly DefaultLoginEndpointHandler<UserKey> _inner;

        public LoginEndpointHandlerBridge(DefaultLoginEndpointHandler<UserKey> inner)
        {
            _inner = inner;
        }

        public Task<IResult> LoginAsync(HttpContext ctx) => _inner.LoginAsync(ctx);
    }
}
