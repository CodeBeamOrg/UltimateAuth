using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
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
    private readonly IUAuthCookieManager _cookieManager;
    private readonly IAuthRedirectResolver _redirectResolver;
    private readonly IRefreshTokenResolver _refreshTokenResolver;
    private readonly IPureJwtLogoutService _pureJwtLogout;
    private readonly IUAuthCookiePolicyBuilder _cookiePolicy;

    public LogoutEndpointHandler(IAuthFlowContextAccessor authContext, IUAuthFlowService flow, IAccessContextFactory accessContextFactory, ISessionApplicationService sessionApplicationService, IUAuthCookieManager cookieManager, IAuthRedirectResolver redirectResolver, IRefreshTokenResolver refreshTokenResolver, IPureJwtLogoutService pureJwtLogout)
        : this(authContext, flow, accessContextFactory, sessionApplicationService, cookieManager, redirectResolver, refreshTokenResolver, pureJwtLogout, new UAuthCookiePolicyBuilder())
    {
    }

    public LogoutEndpointHandler(IAuthFlowContextAccessor authContext, IUAuthFlowService flow, IAccessContextFactory accessContextFactory, ISessionApplicationService sessionApplicationService, IUAuthCookieManager cookieManager, IAuthRedirectResolver redirectResolver, IRefreshTokenResolver refreshTokenResolver, IPureJwtLogoutService pureJwtLogout, IUAuthCookiePolicyBuilder cookiePolicy)
    {
        _authContext = authContext;
        _flow = flow;
        _accessContextFactory = accessContextFactory;
        _sessionApplicationService = sessionApplicationService;
        _cookieManager = cookieManager;
        _redirectResolver = redirectResolver;
        _refreshTokenResolver = refreshTokenResolver;
        _pureJwtLogout = pureJwtLogout;
        _cookiePolicy = cookiePolicy;
    }

    public async Task<IResult> LogoutAsync(HttpContext ctx)
    {
        var authFlow = _authContext.Current;

        if (authFlow.EffectiveMode == UAuthMode.PureJwt)
        {
            var cookieDelivery = authFlow.Response.RefreshTokenDelivery.Mode == GrantDeliveryMode.Cookie;
            var refreshToken = authFlow.IsAuthenticated || cookieDelivery
                ? _refreshTokenResolver.Resolve(ctx)
                : null;
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var accessDelivery = authFlow.Response.AccessTokenDelivery;
                if (!authFlow.IsAuthenticated &&
                    cookieDelivery &&
                    accessDelivery.Mode == GrantDeliveryMode.Header &&
                    accessDelivery.Name is { } accessHeader &&
                    ctx.Request.Headers.ContainsKey(accessHeader))
                {
                    return Results.Unauthorized();
                }

                if (authFlow.IsAuthenticated &&
                    !await _pureJwtLogout.LogoutAsync(authFlow, refreshToken, ctx.RequestAborted))
                {
                    return Results.Unauthorized();
                }
            }
        }
        else if (authFlow.Session is SessionSecurityContext session)
        {
            var request = new LogoutRequest
            {
                SessionId = session.SessionId
            };

            await _flow.LogoutAsync(request, ctx.RequestAborted);
        }

        DeleteIfCookie(ctx, authFlow.Response.SessionIdDelivery);
        if (authFlow.EffectiveMode == UAuthMode.PureJwt &&
            authFlow.Response.RefreshTokenDelivery.Mode == GrantDeliveryMode.Cookie)
        {
            var delivery = authFlow.Response.RefreshTokenDelivery;
            var deletion = _cookiePolicy.Build(delivery, authFlow, GrantKind.RefreshToken);
            deletion.MaxAge = null;
            _cookieManager.Delete(ctx, delivery.Cookie!.Name, deletion);
        }
        else
        {
            DeleteIfCookie(ctx, authFlow.Response.RefreshTokenDelivery);
        }
        DeleteIfCookie(ctx, authFlow.Response.AccessTokenDelivery);

        var decision = _redirectResolver.ResolveSuccess(authFlow, ctx);

        var observedLogout = string.Equals(ctx.Request.Headers["X-UAuth-Logout-Mode"].ToString(), "observe", StringComparison.OrdinalIgnoreCase);

        if (observedLogout)
        {
            return Results.Ok(new
            {
                IsSuccess = true,
                RedirectUrl = decision.Enabled
                    ? decision.TargetUrl
                    : null
            });
        }

        return decision.Enabled
            ? Results.Redirect(decision.TargetUrl!)
            : Results.Ok(new LogoutResponse
            {
                IsSuccess = true
            });
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

        await _sessionApplicationService.LogoutOtherDevicesAsync(access, userKey, request.ExceptChainId, ctx.RequestAborted);
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
            resourceId: userKey.Value);

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

    private void DeleteIfCookie(HttpContext ctx, EffectiveGrantDelivery delivery)
    {
        if (delivery.Mode != GrantDeliveryMode.Cookie)
            return;

        var cookie = delivery.Cookie
            ?? throw new InvalidOperationException($"Cookie options are missing for grant '{delivery.Kind}'.");

        _cookieManager.Delete(ctx, cookie.Name);
    }
}
