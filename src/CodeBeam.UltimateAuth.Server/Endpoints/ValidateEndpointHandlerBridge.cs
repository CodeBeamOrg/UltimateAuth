using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    internal sealed class ValidateEndpointHandlerBridge : IValidateEndpointHandler
    {
        private readonly DefaultValidateEndpointHandler<UserId> _inner;

        public ValidateEndpointHandlerBridge(DefaultValidateEndpointHandler<UserId> inner)
        {
            _inner = inner;
        }

        public Task<IResult> ValidateAsync(HttpContext context, CancellationToken ct = default)
            => _inner.ValidateAsync(context, ct);
    }
}
