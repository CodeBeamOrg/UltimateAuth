using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class AuthFlowEndpointFilter : IEndpointFilter
    {
        private readonly IAuthFlow _authFlow;

        public AuthFlowEndpointFilter(IAuthFlow authFlow)
        {
            _authFlow = authFlow;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<AuthFlowMetadata>();

            if (metadata != null)
            {
                _authFlow.Begin(metadata.FlowType);
            }

            return await next(context);
        }

    }
}
