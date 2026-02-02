using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public sealed class DefaultLogoutEndpointHandler<TUserId> : ILogoutEndpointHandler
    {
        private readonly IAuthFlowContextAccessor _authContext;
        private readonly IUAuthFlowService<TUserId> _flow;
        private readonly IClock _clock;
        private readonly IUAuthCookieManager _cookieManager;
        private readonly AuthRedirectResolver _redirectResolver;

        public DefaultLogoutEndpointHandler(IAuthFlowContextAccessor authContext, IUAuthFlowService<TUserId> flow, IClock clock, IUAuthCookieManager cookieManager, AuthRedirectResolver redirectResolver)
        {
            _authContext = authContext;
            _flow = flow;
            _clock = clock;
            _cookieManager = cookieManager;
            _redirectResolver = redirectResolver;
        }

        public async Task<IResult> LogoutAsync(HttpContext ctx)
        {
            var auth = _authContext.Current;

            if (auth.Session is SessionSecurityContext session)
            {
                var request = new LogoutRequest
                {
                    Tenant = auth.Tenant,
                    SessionId = session.SessionId,
                    At = _clock.UtcNow,
                };

                await _flow.LogoutAsync(request, ctx.RequestAborted);
            }

            DeleteIfCookie(ctx, auth.Response.SessionIdDelivery);
            DeleteIfCookie(ctx, auth.Response.RefreshTokenDelivery);
            DeleteIfCookie(ctx, auth.Response.AccessTokenDelivery);

            if (auth.Response.Logout.RedirectEnabled)
            {
                var redirectUrl = _redirectResolver.ResolveRedirect(ctx, auth.Response.Logout.RedirectPath);
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

            if (delivery.Cookie == null)
                return;

            _cookieManager.Delete(ctx, delivery.Cookie.Name);
        }

    }
}
