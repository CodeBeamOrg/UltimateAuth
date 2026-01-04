using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public sealed class DefaultLogoutEndpointHandler<TUserId> : ILogoutEndpointHandler
    {
        private readonly IUAuthFlowService<TUserId> _flow;
        private readonly IAuthFlowContextFactory _authFlowContextFactory;
        private readonly IAuthResponseResolver _authResponseResolver;
        private readonly IClock _clock;
        private readonly IUAuthCookieManager _cookieManager;
        private readonly AuthRedirectResolver _redirectResolver;

        public DefaultLogoutEndpointHandler(IUAuthFlowService<TUserId> flow, IAuthFlowContextFactory authFlowContextFactory, IAuthResponseResolver authResponseResolver, IClock clock, IUAuthCookieManager cookieManager, AuthRedirectResolver redirectResolver)
        {
            _flow = flow;
            _authFlowContextFactory = authFlowContextFactory;
            _authResponseResolver = authResponseResolver;
            _clock = clock;
            _cookieManager = cookieManager;
            _redirectResolver = redirectResolver;
        }

        public async Task<IResult> LogoutAsync(HttpContext ctx)
        {
            var flowContext = _authFlowContextFactory.Create(ctx, AuthFlowType.Logout);
            var authResponse = _authResponseResolver.Resolve(flowContext);

            var tenantCtx = ctx.GetTenantContext();
            var sessionCtx = ctx.GetSessionContext();

            if (!sessionCtx.IsAnonymous)
            {
                var request = new LogoutRequest
                {
                    TenantId = tenantCtx.TenantId,
                    SessionId = sessionCtx.SessionId!.Value,
                    At = _clock.UtcNow
                };

                await _flow.LogoutAsync(request, ctx.RequestAborted);
            }

            DeleteIfCookie(ctx, authResponse.SessionIdDelivery);
            DeleteIfCookie(ctx, authResponse.RefreshTokenDelivery);
            DeleteIfCookie(ctx, authResponse.AccessTokenDelivery);

            if (authResponse.Logout.RedirectEnabled)
            {
                var redirectUrl = _redirectResolver.ResolveRedirect(ctx, authResponse.Logout.RedirectPath);
                return Results.Redirect(redirectUrl);
            }

            return Results.Ok(new LogoutResponse
            {
                Success = true
            });
        }

        private void DeleteIfCookie(HttpContext ctx, CredentialResponseOptions delivery)
        {
            if (delivery.Mode != TokenResponseMode.Cookie)
                return;

            _cookieManager.Delete(ctx, delivery.Cookie);
        }

    }
}
