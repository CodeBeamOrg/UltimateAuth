using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Services;

namespace CodeBeam.UltimateAuth.IdentityLab.Endpoints;

public static class LabAdminEndpointExtensions
{
    public static IEndpointRouteBuilder MapLabAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/lab/admin").AddEndpointFilter<LabAdminAuthorizationFilter>();

        group.MapGet("/sandboxes", async (
            [AsParameters] SandboxQuery query,
            LabAccessContextProvider contextProvider,
            SandboxAdminService adminService,
            CancellationToken ct) =>
        {
            var context = await contextProvider.CreateAsync(
                "identitylab.sandboxes.admin",
                "identitylab.sandboxes",
                ct);

            var result = await adminService.QueryAsync(
                context, query, ct);

            return Results.Ok(result);
        });

        group.MapGet("/statistics", async (
            LabAccessContextProvider contextProvider,
            SandboxAdminService adminService,
            CancellationToken ct) =>
        {
            var context = await contextProvider.CreateAsync(
                "identitylab.statistics.admin",
                "identitylab.statistics",
                ct);

            var result = await adminService.GetStatisticsAsync(
                context, ct);

            return Results.Ok(result);
        });

        return endpoints;
    }
}
