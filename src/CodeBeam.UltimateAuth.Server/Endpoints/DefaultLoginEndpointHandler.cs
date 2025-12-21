using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using Microsoft.AspNetCore.Http;

public sealed class DefaultLoginEndpointHandler<TUserId> : ILoginEndpointHandler
{
    private readonly IUAuthFlowService<TUserId> _flow;
    private readonly IDeviceResolver _deviceResolver;
    private readonly ITenantResolver _tenantResolver;
    private readonly IClock _clock;
    private readonly IUAuthSessionCookieManager _cookieManager;

    public DefaultLoginEndpointHandler(
        IUAuthFlowService<TUserId> flow,
        IDeviceResolver deviceResolver,
        ITenantResolver tenantResolver,
        IClock clock,
        IUAuthSessionCookieManager cookieManager)
    {
        _flow = flow;
        _deviceResolver = deviceResolver;
        _tenantResolver = tenantResolver;
        _clock = clock;
        _cookieManager = cookieManager;
    }

    public async Task<IResult> LoginAsync(HttpContext ctx)
    {
        if (!ctx.Request.HasFormContentType)
            return Results.BadRequest("Invalid content type.");

        var form = await ctx.Request.ReadFormAsync();

        var request = new LoginRequest
        {
            Identifier = form["Identifier"],
            Secret = form["Secret"]
        };

        if (string.IsNullOrWhiteSpace(request.Identifier) ||
            string.IsNullOrWhiteSpace(request.Secret))
            return Results.Redirect("/login?error=invalid");

        var tenantCtx = ctx.GetTenantContext();

        var flowRequest = request with
        {
            TenantId = tenantCtx.TenantId,
            At = _clock.UtcNow,
            DeviceInfo = _deviceResolver.Resolve(ctx)
        };

        var result = await _flow.LoginAsync(flowRequest, ctx.RequestAborted);

        if (!result.IsSuccess)
            return Results.Redirect("/login?error=invalid");

        _cookieManager.Issue(ctx, result.SessionId!.Value);

        return Results.Redirect("/");
    }

    //public async Task<IResult> LoginAsync(HttpContext ctx)
    //{
    //    var request = await ctx.Request.ReadFromJsonAsync<LoginRequest>();
    //    if (request is null)
    //        return Results.BadRequest("Invalid login request.");

    //    // Middleware should have already resolved the tenant
    //    var tenantCtx = ctx.GetTenantContext();

    //    var flowRequest = request with
    //    {
    //        TenantId = tenantCtx.TenantId,
    //        At = _clock.UtcNow,
    //        DeviceInfo = _deviceResolver.Resolve(ctx)
    //    };

    //    var result = await _flow.LoginAsync(flowRequest, ctx.RequestAborted);

    //    if (result.IsSuccess)
    //    {
    //        _cookieManager.Issue(ctx, result.SessionId.Value);
    //    }

    //    return result.Status switch
    //    {
    //        LoginStatus.Success => Results.Ok(new LoginResponse
    //        {
    //            SessionId = result.SessionId,
    //            AccessToken = result.AccessToken,
    //            RefreshToken = result.RefreshToken
    //        }),

    //        LoginStatus.RequiresContinuation => Results.Ok(new LoginResponse
    //        {
    //            Continuation = result.Continuation
    //        }),

    //        LoginStatus.Failed => Results.Unauthorized(),

    //        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    //    };
    //}
}
