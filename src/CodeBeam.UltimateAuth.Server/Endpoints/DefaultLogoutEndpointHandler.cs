using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Extensions;
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

        public DefaultLogoutEndpointHandler(IUAuthFlowService<TUserId> flow, IClock clock, IUAuthCookieManager cookieManager, IOptions<UAuthServerOptions> options)
        {
            _flow = flow;
            _clock = clock;
            _cookieManager = cookieManager;
            _options = options.Value;
        }

        public async Task<IResult> LogoutAsync(HttpContext ctx)
        {
            var tenantCtx = ctx.GetTenantContext();
            var sessionCtx = ctx.GetSessionContext();

            if (sessionCtx.IsAnonymous)
                return Results.Unauthorized();

            var request = new LogoutRequest
            {
                TenantId = tenantCtx.TenantId,
                SessionId = sessionCtx.SessionId!.Value,
                At = _clock.UtcNow
            };

            await _flow.LogoutAsync(request, ctx.RequestAborted);
            _cookieManager.Delete(ctx);

            var logout = _options.AuthResponse.Logout;

            if (logout.RedirectEnabled)
            {
                var returnUrl = logout.AllowReturnUrlOverride
                    ? ctx.Request.Query["returnUrl"].FirstOrDefault()
                    : null;

                var redirect = !string.IsNullOrWhiteSpace(returnUrl)
                    ? returnUrl
                    : logout.RedirectUrl;

                // TODO: relative / same-origin check
                return Results.Redirect(redirect);
            }

            return Results.Ok(new LogoutResponse
            {
                Success = true
            });
        }
    }
}
