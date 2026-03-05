using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed class UserEndpointHandler : IUserEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IAccessContextFactory _accessContextFactory;
    private readonly IUserApplicationService _users;

    public UserEndpointHandler(IAuthFlowContextAccessor authFlow, IAccessContextFactory accessContextFactory, IUserApplicationService users)
    {
        _authFlow = authFlow;
        _accessContextFactory = accessContextFactory;
        _users = users;
    }

    public async Task<IResult> CreateAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;

        var request = await ctx.ReadJsonAsync<CreateUserRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.Users.CreateAnonymous,
            resource: "users");

        var result = await _users.CreateUserAsync(accessContext, request, ctx.RequestAborted);

        return result.Succeeded
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }

    public async Task<IResult> ChangeStatusSelfAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<ChangeUserStatusSelfRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.Users.ChangeStatusSelf,
            resource: "users",
            resourceId: flow?.UserKey?.Value);

        await _users.ChangeUserStatusAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> ChangeStatusAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<ChangeUserStatusAdminRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.Users.ChangeStatusAdmin,
            resource: "users",
            resourceId: userKey.Value);

        await _users.ChangeUserStatusAsync(accessContext, request, ctx.RequestAborted);
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
            resource: "users",
            resourceId: flow?.UserKey?.Value);

        var profile = await _users.GetMeAsync(accessContext, ctx.RequestAborted);
        return Results.Ok(profile);
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

    public async Task<IResult> UpdateMeAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UpdateProfileRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserProfiles.UpdateSelf,
            resource: "users",
            resourceId: flow?.UserKey?.Value);

        await _users.UpdateUserProfileAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
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

    public async Task<IResult> DeleteAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<DeleteUserRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.Users.DeleteAdmin,
            resource: "users",
            resourceId: userKey.Value,
            attributes: new Dictionary<string, object>
            {
                ["deleteMode"] = request.Mode
            });

        await _users.DeleteUserAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> GetMyIdentifiersAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UserIdentifierQuery>(ctx.RequestAborted) ?? new UserIdentifierQuery();

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.GetSelf,
            resource: "users",
            resourceId: flow.UserKey!.Value);

        var result = await _users.GetIdentifiersByUserAsync(accessContext, request, ctx.RequestAborted);

        return Results.Ok(result);
    }

    public async Task<IResult> GetUserIdentifiersAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UserIdentifierQuery>(ctx.RequestAborted) ?? new UserIdentifierQuery();

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.GetAdmin,
            resource: "users",
            resourceId: userKey.Value);

        var result = await _users.GetIdentifiersByUserAsync(accessContext, request, ctx.RequestAborted);

        return Results.Ok(result);
    }

    public async Task<IResult> IdentifierExistsSelfAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<IdentifierExistsRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.GetSelf,
            resource: "users",
            resourceId: flow.UserKey!.Value);

        var exists = await _users.UserIdentifierExistsAsync(
            accessContext,
            request.Type,
            request.Value,
            IdentifierExistenceScope.WithinUser,
            ctx.RequestAborted);

        return Results.Ok(new IdentifierExistsResponse
        {
            Exists = exists
        });
    }

    public async Task<IResult> IdentifierExistsAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<IdentifierExistsRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.GetAdmin,
            resource: "users",
            resourceId: userKey.Value);

        var exists = await _users.UserIdentifierExistsAsync(
            accessContext,
            request.Type,
            request.Value,
            IdentifierExistenceScope.TenantAny,
            ctx.RequestAborted);

        return Results.Ok(new IdentifierExistsResponse
        {
            Exists = exists
        });
    }

    public async Task<IResult> AddUserIdentifierSelfAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<AddUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.AddSelf,
            resource: "users",
            resourceId: flow.UserKey!.Value);

        await _users.AddUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> AddUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<AddUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.AddAdmin,
            resource: "users",
            resourceId: userKey.Value);

        await _users.AddUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> UpdateUserIdentifierSelfAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UpdateUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.UpdateSelf,
            resource: "users",
            resourceId: flow.UserKey!.Value);

        await _users.UpdateUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> UpdateUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UpdateUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.UpdateAdmin,
            resource: "users",
            resourceId: userKey.Value);

        await _users.UpdateUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> SetPrimaryUserIdentifierSelfAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<SetPrimaryUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.SetPrimarySelf,
            resource: "users",
            resourceId: flow.UserKey!.Value);

        await _users.SetPrimaryUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> SetPrimaryUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<SetPrimaryUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.SetPrimaryAdmin,
            resource: "users",
            resourceId: userKey.Value);

        await _users.SetPrimaryUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> UnsetPrimaryUserIdentifierSelfAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UnsetPrimaryUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.UnsetPrimarySelf,
            resource: "users",
            resourceId: flow.UserKey!.Value);

        await _users.UnsetPrimaryUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> UnsetPrimaryUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UnsetPrimaryUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.UnsetPrimaryAdmin,
            resource: "users",
            resourceId: userKey.Value);

        await _users.UnsetPrimaryUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> VerifyUserIdentifierSelfAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<VerifyUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.VerifySelf,
            resource: "users",
            resourceId: flow.UserKey!.Value);

        await _users.VerifyUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> VerifyUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<VerifyUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.VerifyAdmin,
            resource: "users",
            resourceId: userKey.Value);

        await _users.VerifyUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> DeleteUserIdentifierSelfAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<DeleteUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.DeleteSelf,
            resource: "users",
            resourceId: flow.UserKey!.Value);

        await _users.DeleteUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> DeleteUserIdentifierAdminAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<DeleteUserIdentifierRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserIdentifiers.DeleteAdmin,
            resource: "users",
            resourceId: userKey.Value);

        await _users.DeleteUserIdentifierAsync(accessContext, request, ctx.RequestAborted);
        return Results.Ok();
    }
}
