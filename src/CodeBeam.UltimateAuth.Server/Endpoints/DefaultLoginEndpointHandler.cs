using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
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

        // Middleware should have already resolved the tenant
        var tenantCtx = ctx.GetTenantContext();

        var flowRequest = request with
        {
            TenantId = tenantCtx.TenantId,
            At = _clock.UtcNow,
            DeviceInfo = _deviceResolver.Resolve(ctx)
        };

        var result = await _flow.LoginAsync(flowRequest, ctx.RequestAborted);

        return result.Status switch
        {
            LoginStatus.Success => Results.Ok(new LoginResponse
            {
                SessionId = result.SessionId,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken
            }),

            LoginStatus.RequiresContinuation => Results.Ok(new LoginResponse
            {
                Continuation = result.Continuation
            }),

            LoginStatus.Failed => Results.Unauthorized(),

            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
