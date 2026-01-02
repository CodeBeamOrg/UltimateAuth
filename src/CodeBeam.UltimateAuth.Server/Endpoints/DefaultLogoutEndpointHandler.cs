using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public sealed class DefaultLogoutEndpointHandler<TUserId> : ILogoutEndpointHandler
    {
        private readonly IUAuthFlowService<TUserId> _flow;
        private readonly IClock _clock;
        private readonly IUAuthCookieManager _cookieManager;
        private readonly UAuthServerOptions _options;
        private readonly AuthRedirectResolver _redirectResolver;

        public DefaultLogoutEndpointHandler(IUAuthFlowService<TUserId> flow, IClock clock, IUAuthCookieManager cookieManager, IOptions<UAuthServerOptions> options, AuthRedirectResolver redirectResolver)
        {
            _flow = flow;
            _clock = clock;
            _cookieManager = cookieManager;
            _options = options.Value;
            _redirectResolver = redirectResolver;
        }

        public async Task<IResult> LogoutAsync(HttpContext ctx)
        {
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

            _cookieManager.Delete(ctx);

            var logout = _options.AuthResponse.Logout;

            if (logout.RedirectEnabled)
            {
                var overrideReturnUrl =
                    logout.AllowReturnUrlOverride
                        ? ctx.Request.Query["returnUrl"].FirstOrDefault()
                        : null;

                var redirectUrl = _redirectResolver.ResolveRedirect(
                    ctx,
                    logout.RedirectUrl,
                    string.IsNullOrWhiteSpace(overrideReturnUrl)
                        ? null
                        : new Dictionary<string, string?>
                        {
                            ["returnUrl"] = overrideReturnUrl
                        });

                return Results.Redirect(redirectUrl);
            }

            return Results.Ok(new LogoutResponse
            {
                Success = true
            });
        }

    }
}
