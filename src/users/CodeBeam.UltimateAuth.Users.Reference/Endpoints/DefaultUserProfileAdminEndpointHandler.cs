using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed class DefaultUserProfileAdminEndpointHandler : IUserProfileAdminEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IAccessContextFactory _accessContextFactory;
    private readonly IUserProfileAdminService _profiles;

    public DefaultUserProfileAdminEndpointHandler(IAuthFlowContextAccessor authFlow, IAccessContextFactory accessContextFactory, IUserProfileAdminService profiles)
    {
        _authFlow = authFlow;
        _accessContextFactory = accessContextFactory;
        _profiles = profiles;
    }

    public async Task<IResult> GetAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserProfiles.GetAdmin,
            resource: "users",
            resourceId: userKey.Value
        );

        var result = await _profiles.GetAsync(accessContext, userKey, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> UpdateAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UpdateProfileRequest>(ctx.RequestAborted);

        var accessContext = await _accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.UserProfiles.UpdateAdmin,
            resource: "users",
            resourceId: userKey.Value
        );

        await _profiles.UpdateAsync(accessContext, userKey, request, ctx.RequestAborted);
        return Results.NoContent();
    }
}
