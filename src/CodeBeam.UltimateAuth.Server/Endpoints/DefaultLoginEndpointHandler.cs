using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public sealed class DefaultLoginEndpointHandler<TUserId> : ILoginEndpointHandler
{
    private readonly IUAuthFlowService<TUserId> _flow;
    private readonly IDeviceResolver _deviceResolver;
    private readonly IClock _clock;
    private readonly ICredentialResponseWriter _credentialResponseWriter;
    private readonly IEffectiveServerOptionsProvider _effectiveOptions;
    private readonly AuthRedirectResolver _redirectResolver;

    public DefaultLoginEndpointHandler(
        IUAuthFlowService<TUserId> flow,
        IDeviceResolver deviceResolver,
        IClock clock,
        ICredentialResponseWriter credentialResponseWriter,
        IEffectiveServerOptionsProvider effectiveOptions,
        AuthRedirectResolver redirectResolver)
    {
        _flow = flow;
        _deviceResolver = deviceResolver;
        _clock = clock;
        _credentialResponseWriter = credentialResponseWriter;
        _effectiveOptions = effectiveOptions;
        _redirectResolver = redirectResolver;
    }

    public async Task<IResult> LoginAsync(HttpContext ctx)
    {
        var options = _effectiveOptions.Get(ctx, AuthFlowType.Login);

        if (!ctx.Request.HasFormContentType)
            return Results.BadRequest("Invalid content type.");

        var form = await ctx.Request.ReadFormAsync();

        var identifier = form["Identifier"].ToString();
        var secret = form["Secret"].ToString();

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(secret))
            return RedirectFailure(ctx, AuthFailureReason.InvalidCredentials, options);

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
            return RedirectFailure(ctx, result.FailureReason ?? AuthFailureReason.Unknown, options);

        if (result.SessionId is not null)
        {
            _credentialResponseWriter.Write(ctx, result.SessionId.Value, options.AuthResponse.SessionIdDelivery);
        }
        else if (result.AccessToken is not null)
        {
            _credentialResponseWriter.Write(ctx, result.AccessToken.Token, options.AuthResponse.AccessTokenDelivery);
        }

        if (result.RefreshToken is not null)
        {
            _credentialResponseWriter.Write(ctx, result.RefreshToken.Token, options.AuthResponse.RefreshTokenDelivery);
        }

        if (options.AuthResponse.Login.RedirectEnabled)
        {
            var redirectUrl = _redirectResolver.ResolveRedirect(ctx, options.AuthResponse.Login.SuccessRedirect);
            return Results.Redirect(redirectUrl);
        }

        // TODO: Add PKCE, return result with body

        return Results.Ok();
    }

    private IResult RedirectFailure(HttpContext ctx, AuthFailureReason reason, UAuthServerOptions options)
    {
        var login = options.AuthResponse.Login;

        var code =
            login.FailureCodes != null &&
            login.FailureCodes.TryGetValue(reason, out var mapped)
                ? mapped
                : "failed";

        var redirectUrl = _redirectResolver.ResolveRedirect(
            ctx,
            login.FailureRedirect,
            new Dictionary<string, string?>
            {
                [login.FailureQueryKey] = code
            });

        return Results.Redirect(redirectUrl);
    }
}
