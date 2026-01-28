using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed class DefaultUserEndpointHandler : IUserEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IAccessContextFactory _accessContextFactory;
    private readonly IUserApplicationService _users;

    public DefaultUserEndpointHandler(IAuthFlowContextAccessor authFlow, IAccessContextFactory accessContextFactory, IUserApplicationService users)
    {
        _authFlow = authFlow;
        _accessContextFactory = accessContextFactory;
        _users = users;
    }

    public async Task<IResult> CreateAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<CreateUserRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.Users.Create,
            resource: "users");

        var result = await _users.CreateUserAsync(accessContext, request, ctx.RequestAborted);

        return result.Succeeded
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }

    public async Task<IResult> ChangeStatusAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<ChangeUserStatusRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.Users.ChangeStatus,
            resource: "users",
            resourceId: request.UserKey.Value);

        await _users.ChangeUserStatusAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> DeleteAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<DeleteUserRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.Users.Delete,
            resource: "users",
            resourceId: request.UserKey.Value,
            attributes: new Dictionary<string, object>
            {
                ["deleteMode"] = request.Mode
            });

        await _users.DeleteUserAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> GetMeAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserProfiles.GetSelf,
            resource: "users");

        var profile = await _users.GetMeAsync(accessContext, ctx.RequestAborted);
        return Results.Ok(profile);
    }

    public async Task<IResult> UpdateMeAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UpdateProfileRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserProfiles.UpdateSelf,
            resource: "users");

        await _users.UpdateUserProfileAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> GetUserAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserProfiles.GetAdmin,
            resource: "users",
            resourceId: userKey.Value);

        var profile = await _users.GetUserProfileAsync(accessContext, ctx.RequestAborted);
        return Results.Ok(profile);
    }

    public async Task<IResult> UpdateUserAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UpdateProfileRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserProfiles.UpdateAdmin,
            resource: "users",
            resourceId: userKey.Value);

        await _users.UpdateUserProfileAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }
}
