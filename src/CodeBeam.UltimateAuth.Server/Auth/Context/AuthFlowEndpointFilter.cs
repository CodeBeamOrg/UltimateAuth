using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class AuthFlowEndpointFilter : IEndpointFilter
{
    private readonly IAuthFlow _authFlow;

    public AuthFlowEndpointFilter(IAuthFlow authFlow)
    {
        _authFlow = authFlow;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();

        var metadata = endpoint?.Metadata.GetMetadata<AuthFlowMetadata>();

        AuthFlowContext? flowContext = null;

        if (metadata != null)
        {
            flowContext = await _authFlow.BeginAsync(metadata.FlowType);
        }

        var requiresAuthentication = endpoint?.Metadata
            .GetMetadata<UAuthRequireAuthenticationMetadata>()
            is not null;

        if (requiresAuthentication && flowContext?.IsAuthenticated != true)
        {
            context.HttpContext.Response.Headers[UAuthConstants.Headers.AuthRecovery] = "challenge";
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
