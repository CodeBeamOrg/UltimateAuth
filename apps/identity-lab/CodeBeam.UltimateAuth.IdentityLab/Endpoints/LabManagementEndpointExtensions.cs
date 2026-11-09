using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Endpoints;

public sealed record LabOperation(string Action, string Resource);
public sealed record DemoRoleRequest(string UserKey, string Role, bool Assigned);
public sealed record DemoPermissionRequest(string Permission);

public static class LabManagementEndpointExtensions
{
    public static IEndpointRouteBuilder MapLabManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/lab/manage").AddEndpointFilter<LabManagementAuthorizationFilter>();
        group.MapPost("/users", async (CreateUserRequest request, HttpContext http, SandboxUserService service, CancellationToken ct) =>
            Results.Ok(await service.CreateAsync(LabManagementAuthorizationFilter.Context(http), request, ct)))
            .WithMetadata(new LabOperation(UAuthActions.Users.CreateAdmin, "users"));

        group.MapPost("/owner-repair/status", async (HttpContext http, LabOwnerRepairService service, CancellationToken ct) =>
            Results.Ok(await service.GetStatusAsync(LabManagementAuthorizationFilter.Context(http), ct)))
            .WithMetadata(new LabOperation("identitylab.owner.repair.status", "identitylab.sandbox"));
        group.MapPost("/owner-repair", async (HttpContext http, LabOwnerRepairService service, CancellationToken ct) =>
            Results.Ok(await service.RepairAsync(LabManagementAuthorizationFilter.Context(http), ct)))
            .WithMetadata(new LabOperation("identitylab.owner.repair", "identitylab.sandbox"));

        group.MapPost("/demo-roles/query", async (DemoRoleRequest request, HttpContext http, LabDemoRoleService service, CancellationToken ct) =>
            Results.Ok(await service.GetRolesAsync(LabManagementAuthorizationFilter.Context(http), UserKey.FromString(request.UserKey), ct)))
            .WithMetadata(new LabOperation(UAuthActions.Authorization.Roles.GetAdmin, "authorization.roles"));
        group.MapPost("/demo-roles", async (DemoRoleRequest request, HttpContext http, LabDemoRoleService service, CancellationToken ct) =>
        {
            await service.SetRoleAsync(LabManagementAuthorizationFilter.Context(http), UserKey.FromString(request.UserKey), request.Role, request.Assigned, ct);
            return Results.Ok(true);
        }).WithMetadata(new LabOperation(UAuthActions.Authorization.Roles.AssignAdmin, "authorization.roles"));
        group.MapPost("/demo-permissions", async (HttpContext http, LabDemoStoreService service, CancellationToken ct) =>
            Results.Ok(await service.GetPermissionsAsync(LabManagementAuthorizationFilter.Context(http), ct)))
            .WithMetadata(new LabOperation(LabDemoPermissions.ProductsRead, "demo.products"));
        group.MapPost("/demo-permissions/check", async (DemoPermissionRequest request, HttpContext http, LabDemoStoreService service, CancellationToken ct) =>
        {
            await service.RequireAsync(LabManagementAuthorizationFilter.Context(http), request.Permission, ct);
            return Results.Ok(true);
        }).WithMetadata(new LabOperation(LabDemoPermissions.ProductsRead, "demo.products"));
        return endpoints;
    }
}
