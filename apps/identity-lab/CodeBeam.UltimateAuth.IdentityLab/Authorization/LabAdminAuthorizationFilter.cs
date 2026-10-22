using CodeBeam.UltimateAuth.IdentityLab.Authorization;

namespace CodeBeam.UltimateAuth.IdentityLab.Endpoints;

public sealed class LabAdminAuthorizationFilter(LabAccessContextProvider contextProvider, ILabAdminGuard adminGuard) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext invocation,
        EndpointFilterDelegate next)
    {
        var context = await contextProvider.CreateAsync(
            "identitylab.admin",
            "identitylab",
            invocation.HttpContext.RequestAborted);

        if (!context.IsAuthenticated || context.ActorUserKey is null)
            return Results.Unauthorized();

        if (!await adminGuard.IsAdminAsync(
            context,
            invocation.HttpContext.RequestAborted))
        {
            return Results.Forbid();
        }

        return await next(invocation);
    }
}
