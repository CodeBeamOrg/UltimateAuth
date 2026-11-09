using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using Microsoft.AspNetCore.Antiforgery;

namespace CodeBeam.UltimateAuth.IdentityLab.Endpoints;

// A request scope owns the validated context. No component state or payload supplies the actor.
public sealed class LabManagementAuthorizationFilter(
    LabAccessContextProvider contextProvider,
    IAntiforgery antiforgery,
    ILogger<LabManagementAuthorizationFilter> logger) : IEndpointFilter
{
    public static AccessContext Context(HttpContext http) => (AccessContext)http.Items[typeof(AccessContext)]!;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var http = invocation.HttpContext;
        try
        {
            if (!HttpMethods.IsGet(http.Request.Method))
            {
                try { await antiforgery.ValidateRequestAsync(http); }
                catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            }

            var operation = http.GetEndpoint()!.Metadata.GetMetadata<LabOperation>()!;
            var context = await contextProvider.CreateAsync(operation.Action, operation.Resource, http.RequestAborted);
            if (!context.IsAuthenticated || context.ActorUserKey is null)
                return Results.Unauthorized();
            if (context.IsSystemActor || context.ActorTenant != TenantKeys.Single || context.IsCrossTenant)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            http.Items[typeof(AccessContext)] = context;
            return await next(invocation);
        }
        catch (UnauthorizedAccessException) { return Results.StatusCode(StatusCodes.Status403Forbidden); }
        catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Identity Lab management request failed for {Path}", http.Request.Path);
            return Results.Problem("Unable to complete the operation.");
        }
    }
}
