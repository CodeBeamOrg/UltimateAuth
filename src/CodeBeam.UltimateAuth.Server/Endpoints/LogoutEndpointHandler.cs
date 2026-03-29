using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public sealed class LogoutEndpointHandler : ILogoutEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IUAuthFlowService _flow;
    private readonly IAccessContextFactory _accessContextFactory;
    private readonly ISessionApplicationService _sessionApplicationService;
    private readonly IClock _clock;
    private readonly IUAuthCookieManager _cookieManager;
    private readonly IAuthRedirectResolver _redirectResolver;

    public LogoutEndpointHandler(IAuthFlowContextAccessor authContext, IUAuthFlowService flow, IAccessContextFactory accessContextFactory, ISessionApplicationService sessionApplicationService, IClock clock, IUAuthCookieManager cookieManager, IAuthRedirectResolver redirectResolver)
    {
        _authContext = authContext;
        _flow = flow;
        _accessContextFactory = accessContextFactory;
        _sessionApplicationService = sessionApplicationService;
        _clock = clock;
        _cookieManager = cookieManager;
        _redirectResolver = redirectResolver;
    }

    public async Task<IResult> LogoutAsync(HttpContext ctx)
    {
        var authFlow = _authContext.Current;

        if (authFlow.Session is SessionSecurityContext session)
        {
            var request = new LogoutRequest
            {
                SessionId = session.SessionId
            };

            await _flow.LogoutAsync(request, ctx.RequestAborted);
        }

        DeleteIfCookie(ctx, authFlow.Response.SessionIdDelivery);
        DeleteIfCookie(ctx, authFlow.Response.RefreshTokenDelivery);
        DeleteIfCookie(ctx, authFlow.Response.AccessTokenDelivery);

        var decision = _redirectResolver.ResolveSuccess(authFlow, ctx);

        return decision.Enabled
            ? Results.Redirect(decision.TargetUrl!)
            : Results.Ok(new LogoutResponse { Success = true });
    }

    public async Task<IResult> LogoutDeviceSelfAsync(HttpContext ctx)
    {
        var flow = _authContext.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        if (flow.UserKey is not UserKey userKey)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<LogoutDeviceRequest>(ctx.RequestAborted);

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Flows.LogoutDeviceSelf,
            resource: "flows",
            resourceId: userKey.Value);

        var result = await _sessionApplicationService.LogoutDeviceAsync(access, request.ChainId, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> LogoutDeviceAdminAsync(HttpContext ctx, UserKey userKey)
    {
        var flow = _authContext.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<LogoutDeviceRequest>(ctx.RequestAborted);

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Flows.LogoutDeviceAdmin,
            resource: "flows",
            resourceId: userKey.Value);

        var result = await _sessionApplicationService.LogoutDeviceAsync(access, request.ChainId, ctx.RequestAborted);
        return Results.Ok(result);
    }

    public async Task<IResult> LogoutOthersSelfAsync(HttpContext ctx)
    {
        var flow = _authContext.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        if (flow.UserKey is not UserKey userKey)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Flows.LogoutOthersSelf,
            resource: "flows",
            resourceId: userKey.Value);

        if (access.ActorChainId is not SessionChainId chainId)
            return Results.Unauthorized();

        await _sessionApplicationService.LogoutOtherDevicesAsync(access, userKey, chainId, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> LogoutOthersAdminAsync(HttpContext ctx, UserKey userKey)
    {
        var flow = _authContext.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var request = await ctx.ReadJsonAsync<LogoutOtherDevicesRequest>(ctx.RequestAborted);

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Flows.LogoutOthersAdmin,
            resource: "flows",
            resourceId: userKey.Value);

        await _sessionApplicationService.LogoutOtherDevicesAsync(access, userKey, request.CurrentChainId, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> LogoutAllSelfAsync(HttpContext ctx)
    {
        var flow = _authContext.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        if (flow.UserKey is not UserKey userKey)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Flows.LogoutAllSelf,
            resource: "flows",
            resourceId: userKey);

        await _sessionApplicationService.LogoutAllDevicesAsync(access, userKey, ctx.RequestAborted);
        return Results.Ok();
    }

    public async Task<IResult> LogoutAllAdminAsync(HttpContext ctx, UserKey userKey)
    {
        var flow = _authContext.Current;
        if (!flow.IsAuthenticated)
            return Results.Unauthorized();

        var access = await _accessContextFactory.CreateAsync(
            flow,
            UAuthActions.Flows.LogoutAllAdmin,
            resource: "flows",
            resourceId: userKey.Value);

        await _sessionApplicationService.LogoutAllDevicesAsync(access, userKey, ctx.RequestAborted);
        return Results.Ok();
    }

    private void DeleteIfCookie(HttpContext ctx, CredentialResponseOptions delivery)
    {
        if (delivery.Mode != TokenResponseMode.Cookie)
            return;

        if (delivery.Cookie == null)
            return;

        _cookieManager.Delete(ctx, delivery.Cookie.Name);
    }
}
