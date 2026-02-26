using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public sealed class LogoutEndpointHandler : ILogoutEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IUAuthFlowService _flow;
    private readonly IClock _clock;
    private readonly IUAuthCookieManager _cookieManager;
    private readonly IAuthRedirectResolver _redirectResolver;

    public LogoutEndpointHandler(IAuthFlowContextAccessor authContext, IUAuthFlowService flow, IClock clock, IUAuthCookieManager cookieManager, IAuthRedirectResolver redirectResolver)
    {
        _authContext = authContext;
        _flow = flow;
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
                Tenant = authFlow.Tenant,
                SessionId = session.SessionId,
                At = _clock.UtcNow,
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

    private void DeleteIfCookie(HttpContext ctx, CredentialResponseOptions delivery)
    {
        if (delivery.Mode != TokenResponseMode.Cookie)
            return;

        if (delivery.Cookie == null)
            return;

        _cookieManager.Delete(ctx, delivery.Cookie.Name);
    }
}
