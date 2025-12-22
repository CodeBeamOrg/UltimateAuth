using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

public sealed class DefaultLoginEndpointHandler<TUserId> : ILoginEndpointHandler
{
    private readonly IUAuthFlowService<TUserId> _flow;
    private readonly IDeviceResolver _deviceResolver;
    private readonly ITenantResolver _tenantResolver;
    private readonly IClock _clock;
    private readonly IUAuthCookieManager _cookieManager;
    private readonly ICredentialResponseWriter _credentialResponseWriter;
    private readonly UAuthServerOptions _options;

    public DefaultLoginEndpointHandler(
        IUAuthFlowService<TUserId> flow,
        IDeviceResolver deviceResolver,
        ITenantResolver tenantResolver,
        IClock clock,
        IUAuthCookieManager cookieManager,
        ICredentialResponseWriter credentialResponseWriter,
        IOptions<UAuthServerOptions> options)
    {
        _flow = flow;
        _deviceResolver = deviceResolver;
        _tenantResolver = tenantResolver;
        _clock = clock;
        _cookieManager = cookieManager;
        _credentialResponseWriter = credentialResponseWriter;
        _options = options.Value;
    }

    public async Task<IResult> LoginAsync(HttpContext ctx)
    {
        if (!ctx.Request.HasFormContentType)
            return Results.BadRequest("Invalid content type.");

        var form = await ctx.Request.ReadFormAsync();

        var identifier = form["Identifier"].ToString();
        var secret = form["Secret"].ToString();

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(secret))
            return RedirectFailure(AuthFailureReason.InvalidCredentials);

        var tenantCtx = ctx.GetTenantContext();

        var flowRequest = new LoginRequest
        {
            Identifier = identifier,
            Secret = secret,
            TenantId = tenantCtx.TenantId,
            At = _clock.UtcNow,
            DeviceInfo = _deviceResolver.Resolve(ctx)
        };

        var result = await _flow.LoginAsync(flowRequest, ctx.RequestAborted);

        if (!result.IsSuccess)
            return RedirectFailure(result.FailureReason ?? AuthFailureReason.Unknown);

        if (result.SessionId is not null)
        {
            _credentialResponseWriter.Write(ctx, result.SessionId.Value, _options.AuthResponse.SessionIdDelivery);
        }
        else if (result.AccessToken is not null)
        {
            _credentialResponseWriter.Write(ctx, result.AccessToken.Token, _options.AuthResponse.AccessTokenDelivery);
        }

        if (result.RefreshToken is not null)
        {
            _credentialResponseWriter.Write(ctx, result.RefreshToken.Token, _options.AuthResponse.RefreshTokenDelivery);
        }

        if (_options.AuthResponse.Redirect.Enabled)
        {
            return Results.Redirect(_options.AuthResponse.Redirect.SuccessRedirect);
        }

        // TODO: Add PKCE, return result with body

        return Results.Ok();
    }

    private IResult RedirectFailure(AuthFailureReason reason)
    {
        var redirect = _options.AuthResponse?.Redirect ?? new RedirectResponseOptions();

        var code = (redirect.FailureCodes != null && redirect.FailureCodes.TryGetValue(reason, out var c))
            ? c
            : "failed";

        return Results.Redirect($"{redirect.FailureRedirect}?{redirect.FailureQueryKey}={code}");
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
