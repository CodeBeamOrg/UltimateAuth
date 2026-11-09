using CodeBeam.UltimateAuth.IdentityLab.Authorization;

namespace CodeBeam.UltimateAuth.IdentityLab.Endpoints;

public sealed class LabAdminAuthorizationFilter(ILabAdminGuard adminGuard) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext invocation,
        EndpointFilterDelegate next)
    {
        var context = LabManagementAuthorizationFilter.Context(invocation.HttpContext);

        if (!context.IsAuthenticated || context.ActorUserKey is null)
            return Results.Unauthorized();

        if (!await adminGuard.IsAdminAsync(
            context,
            invocation.HttpContext.RequestAborted))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return await next(invocation);
    }
}
