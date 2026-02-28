using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class SessionEndpointHandler : ISessionEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IAccessContextFactory _accessContextFactory;
    private readonly ISessionApplicationService _sessions;

    public SessionEndpointHandler(IAuthFlowContextAccessor authFlow, IAccessContextFactory accessContextFactory, ISessionApplicationService sessions)
    {
        _authFlow = authFlow;
        _accessContextFactory = accessContextFactory;
        _sessions = sessions;
    }

    public async Task<IResult> GetMyChainsAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<PageRequest>(ctx.RequestAborted) ?? new PageRequest();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.ListChainsSelf,
            resource: "sessions",
            resourceId: flow.UserKey?.Value);

        var result = await _sessions.GetUserChainsAsync(access, flow.UserKey!.Value, request, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> GetMyChainDetailAsync(SessionChainId chainId, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.GetChainSelf,
            resource: "sessions",
            resourceId: flow.UserKey?.Value);

        var result = await _sessions.GetUserChainDetailAsync(access, flow.UserKey!.Value, chainId, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> RevokeMyChainAsync(SessionChainId chainId, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.RevokeChainSelf,
            resource: "sessions",
            resourceId: flow.UserKey?.Value);

        await _sessions.RevokeUserChainAsync(access, flow.UserKey!.Value, chainId, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> RevokeOtherChainsAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated || flow.Session == null)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.RevokeOtherChainsSelf,
            resource: "sessions",
            resourceId: flow.UserKey?.Value);

        await _sessions.RevokeOtherChainsAsync(access, flow.UserKey!.Value, flow.Session.ChainId, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> RevokeAllMyChainsAsync(HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.RevokeAllChainsSelf,
            resource: "sessions",
            resourceId: flow.UserKey?.Value);

        await _sessions.RevokeAllChainsAsync(access, flow.UserKey!.Value, null, ctx.RequestAborted);
        return Results.Ok();
    }


    public async Task<IResult> GetUserChainsAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<PageRequest>(ctx.RequestAborted) ?? new PageRequest();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.ListChainsAdmin,
            resource: "sessions",
            resourceId: userKey.Value);

        var result = await _sessions.GetUserChainsAsync(access, userKey, request, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> GetUserChainDetailAsync(UserKey userKey, SessionChainId chainId, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.GetChainAdmin,
            resource: "sessions",
            resourceId: userKey.Value);

        var result = await _sessions.GetUserChainDetailAsync(access, userKey, chainId, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> RevokeUserSessionAsync(UserKey userKey, AuthSessionId sessionId, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.RevokeSessionAdmin,
            resource: "sessions",
            resourceId: userKey.Value);

        await _sessions.RevokeUserSessionAsync(access, userKey, sessionId, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> RevokeUserChainAsync(UserKey userKey, SessionChainId chainId, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.RevokeChainAdmin,
            resource: "sessions",
            resourceId: userKey.Value);

        await _sessions.RevokeUserChainAsync(access, userKey, chainId, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> RevokeAllChainsAsync(UserKey userKey, SessionChainId? exceptChainId, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.RevokeAllChainsAdmin,
            resource: "sessions",
            resourceId: userKey.Value);

        await _sessions.RevokeAllChainsAsync(access, userKey, exceptChainId, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> RevokeRootAsync(UserKey userKey, HttpContext ctx)
    {
        var flow = _authFlow.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Sessions.RevokeRootAdmin,
            resource: "sessions",
            resourceId: userKey.Value);

        await _sessions.RevokeRootAsync(access, userKey, ctx.RequestAborted);
        return Results.Ok();
    }
}
