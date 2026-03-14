using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

public sealed class AuthorizationEndpointHandler : IAuthorizationEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IAuthorizationService _authorization;
    private readonly IUserRoleService _userRoles;
    private readonly IRoleService _roles;
    private readonly IAccessContextFactory _accessContextFactory;

    public AuthorizationEndpointHandler(
        IAuthFlowContextAccessor authFlow,
        IAuthorizationService authorization,
        IUserRoleService userRoles,
        IRoleService roles,
        IAccessContextFactory accessContextFactory)
    {
        _authFlow = authFlow;
        _authorization = authorization;
        _userRoles = userRoles;
        _roles = roles;
        _accessContextFactory = accessContextFactory;
    }


    public async Task<IResult> CheckAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<AuthorizationCheckRequest>(ctx.RequestAborted);

        if (string.IsNullOrWhiteSpace(req.Resource))
            return Results.BadRequest("Resource is required for authorization check.");

        if (string.IsNullOrWhiteSpace(req.Action))
            return Results.BadRequest("Action is required for authorization check.");

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: req.Action,
            resource: req.Resource,
            resourceId: req.ResourceId
        );

        var result = await _authorization.AuthorizeAsync(accessContext, ctx.RequestAborted);

        if (result.RequiresReauthentication)
            return Results.StatusCode(StatusCodes.Status428PreconditionRequired);

        return result.IsAllowed
            ? Results.Ok(result)
            : Results.Forbid();
    }

    public async Task<IResult> GetMyRolesAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<RoleQuery>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Authorization.Roles.GetSelf,
            resource: "authorization.roles",
            resourceId: flow.UserKey!.Value
        );

        var roles = await _userRoles.GetRolesAsync(accessContext, flow.UserKey!.Value, req, ctx.RequestAborted);
        return Results.Ok(new UserRolesResponse
        {
            UserKey = flow.UserKey!.Value,
            Roles = roles
        });

    }

    public async Task<IResult> GetUserRolesAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<RoleQuery>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Authorization.Roles.GetAdmin,
            resource: "authorization.roles",
            resourceId: userKey.Value
        );

        var roles = await _userRoles.GetRolesAsync(accessContext, userKey, req, ctx.RequestAborted);

        return Results.Ok(new UserRolesResponse
        {
            UserKey = userKey,
            Roles = roles
        });
    }

    public async Task<IResult> AssignRoleAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<AssignRoleRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Authorization.Roles.AssignAdmin,
            resource: "authorization.roles",
            resourceId: userKey.Value
        );

        await _userRoles.AssignAsync(accessContext, userKey, req.Role, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> RemoveRoleAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<AssignRoleRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Authorization.Roles.RemoveAdmin,
            resource: "authorization.roles",
            resourceId: userKey.Value
        );

        await _userRoles.RemoveAsync(accessContext, userKey, req.Role, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> CreateRoleAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<CreateRoleRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Authorization.Roles.CreateAdmin,
            resource: "authorization.roles"
        );

        var role = await _roles.CreateAsync(accessContext, req.Name, req.Permissions, ctx.RequestAborted);

        return Results.Ok(role);
    }

    public async Task<IResult> RenameRoleAsync(RoleId roleId, HttpContext ctx)
    {
        var flow = _authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<RenameRoleRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Authorization.Roles.RenameAdmin,
            resource: "authorization.roles",
            resourceId: roleId.ToString()
        );

        await _roles.RenameAsync(accessContext, roleId, req.Name, ctx.RequestAborted);

        return Results.Ok();
    }

    public async Task<IResult> DeleteRoleAsync(RoleId roleId, HttpContext ctx)
    {
        var flow = _authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<DeleteRoleRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Authorization.Roles.DeleteAdmin,
            resource: "authorization.roles",
            resourceId: roleId.ToString()
        );

        var result = await _roles.DeleteAsync(accessContext, roleId, req.Mode, ctx.RequestAborted);

        return Results.Ok(result);
    }
    public async Task<IResult> SetRolePermissionsAsync(RoleId roleId, HttpContext ctx)
    {
        var flow = _authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<SetPermissionsRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Authorization.Roles.SetPermissionsAdmin,
            resource: "authorization.roles",
            resourceId: roleId.ToString()
        );

        await _roles.SetPermissionsAsync(accessContext, roleId, req.Permissions, ctx.RequestAborted);

        return Results.Ok();
    }

    public async Task<IResult> QueryRolesAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var req = await ctx.ReadJsonAsync<RoleQuery>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            flow,
            action: UAuthActions.Authorization.Roles.QueryAdmin,
            resource: "authorization.roles"
        );

        var result = await _roles.QueryAsync(accessContext, req, ctx.RequestAborted);

        return Results.Ok(result);
    }
}
