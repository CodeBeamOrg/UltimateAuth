using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Services;

namespace CodeBeam.UltimateAuth.IdentityLab.Endpoints;

public static class LabAdminEndpointExtensions
{
    public static IEndpointRouteBuilder MapLabAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/lab/admin")
            .AddEndpointFilter<LabManagementAuthorizationFilter>()
            .AddEndpointFilter<LabAdminAuthorizationFilter>()
            .WithMetadata(new LabOperation("identitylab.admin", "identitylab"));

        group.MapGet("/sandboxes", async (
            [AsParameters] SandboxQuery query,
            HttpContext http,
            SandboxAdminService adminService,
            CancellationToken ct) =>
        {
            var context = LabManagementAuthorizationFilter.Context(http);

            var result = await adminService.QueryAsync(
                context, query, ct);

            return Results.Ok(result);
        }).WithMetadata(new LabOperation("identitylab.sandboxes.admin", "identitylab.sandboxes"));

        group.MapGet("/statistics", async (
            HttpContext http,
            SandboxAdminService adminService,
            CancellationToken ct) =>
        {
            var context = LabManagementAuthorizationFilter.Context(http);

            var result = await adminService.GetStatisticsAsync(
                context, ct);

            return Results.Ok(result);
        }).WithMetadata(new LabOperation("identitylab.statistics.admin", "identitylab.statistics"));

        // POST queries reuse the existing CSRF-aware JSON browser transport.
        group.MapPost("/repair/scan", async (HttpContext http, LabRepairService service, CancellationToken ct) =>
            Results.Ok(await service.GetSnapshotAsync(LabManagementAuthorizationFilter.Context(http), ct)))
            .WithMetadata(new LabOperation("identitylab.repair.admin", "identitylab.repair"));
        group.MapPost("/repair/owner/{userKey}", async (string userKey, HttpContext http, LabRepairService service, CancellationToken ct) =>
            Results.Ok(await service.RepairOwnerAsync(LabManagementAuthorizationFilter.Context(http), UserKey.FromString(userKey), ct)))
            .WithMetadata(new LabOperation("identitylab.repair.admin", "identitylab.repair"));
        group.MapPost("/repair/sandbox/{sandboxId:guid}/delete", async (Guid sandboxId, HttpContext http, LabRepairService service, CancellationToken ct) =>
            Results.Ok(await service.DeleteEmptySandboxAsync(LabManagementAuthorizationFilter.Context(http), sandboxId, ct)))
            .WithMetadata(new LabOperation("identitylab.repair.admin", "identitylab.repair"));

        return endpoints;
    }
}
