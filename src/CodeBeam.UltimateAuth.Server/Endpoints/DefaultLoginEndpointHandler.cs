using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using Microsoft.AspNetCore.Http;

public sealed class DefaultLoginEndpointHandler<TUserId> : ILoginEndpointHandler
{
    private readonly IUAuthFlowService _flow;
    private readonly IDeviceResolver _deviceResolver;
    private readonly ITenantResolver _tenantResolver;
    private readonly IClock _clock;

    public DefaultLoginEndpointHandler(
        IUAuthFlowService flow,
        IDeviceResolver deviceResolver,
        ITenantResolver tenantResolver,
        IClock clock)
    {
        _flow = flow;
        _deviceResolver = deviceResolver;
        _tenantResolver = tenantResolver;
        _clock = clock;
    }

    public async Task<IResult> LoginAsync(HttpContext ctx)
    {
        var request = await ctx.Request.ReadFromJsonAsync<LoginRequest>();
        if (request is null)
            return Results.BadRequest("Invalid login request.");

        var tenantCtx = await _tenantResolver.ResolveAsync(ctx);

        var flowRequest = request with
        {
            TenantId = tenantCtx.TenantId,
            At = _clock.UtcNow,
            DeviceInfo = _deviceResolver.Resolve(ctx)
        };

        var result = await _flow.LoginAsync(flowRequest, ctx.RequestAborted);

        return result.Status switch
        {
            LoginStatus.Success => Results.Ok(new
            {
                sessionId = result.SessionId,
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken
            }),

            LoginStatus.RequiresContinuation => Results.Accepted(null, result.Continuation),

            LoginStatus.Failed => Results.Unauthorized(),

            _ => Results.StatusCode(500)
        };
    }
}
