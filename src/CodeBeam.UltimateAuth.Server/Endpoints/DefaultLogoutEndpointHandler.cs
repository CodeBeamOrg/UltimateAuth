using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public sealed class DefaultLogoutEndpointHandler<TUserId> : ILogoutEndpointHandler
    {
        private readonly IUAuthFlowService _flow;
        private readonly IClock _clock;

        public DefaultLogoutEndpointHandler(IUAuthFlowService flow, IClock clock)
        {
            _flow = flow;
            _clock = clock;
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

            return Results.Ok(new LogoutResponse
            {
                Success = true
            });
        }
    }
}
