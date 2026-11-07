using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Security;

internal sealed class UAuthCsrfEndpointFilter(IAuthFlowContextAccessor authContext, UAuthCsrfPolicyEvaluator policy, UAuthCsrfValidator validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;

        var metadata = http.GetEndpoint()?.Metadata.GetMetadata<UAuthCsrfMetadata>();

        if (metadata is null)
            return await next(context);

        var auth = authContext.Current;

        if (!policy.RequiresProtection(auth, metadata.Operation))
        {
            return await next(context);
        }

        if (!await validator.ValidateAsync(http))
        {
            return Results.BadRequest(
                new
                {
                    error = "invalid_antiforgery_token"
                });
        }

        return await next(context);
    }
}
