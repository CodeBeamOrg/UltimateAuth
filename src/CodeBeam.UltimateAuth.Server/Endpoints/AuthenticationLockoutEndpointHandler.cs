using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Authentication;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class AuthenticationLockoutEndpointHandler(
    IAuthFlowContextAccessor authFlow,
    IAccessContextFactory accessContextFactory,
    IAuthenticationLockoutService lockouts)
    : IAuthenticationLockoutEndpointHandler
{
    public async Task<IResult> GetAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<LockoutQueryRequest>(ctx.RequestAborted);

        var access = await accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.Authentication.GetLockoutAdmin,
            resource: "authentication",
            resourceId: userKey.Value);

        var result = await lockouts.GetAsync(access, request, ctx.RequestAborted);

        ctx.Response.Headers.CacheControl = "no-store";

        return Results.Ok(result);
    }

    public async Task<IResult> UnlockAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = authFlow.Current;

        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<UnlockAuthenticationRequest>(ctx.RequestAborted);

        var access = await accessContextFactory.CreateAsync(
            authFlow: flow,
            action: UAuthActions.Authentication.UnlockAdmin,
            resource: "authentication",
            resourceId: userKey.Value);

        var result = await lockouts.UnlockAsync(access, request, ctx.RequestAborted);

        ctx.Response.Headers.CacheControl = "no-store";

        return Results.Ok(result);
    }
}
