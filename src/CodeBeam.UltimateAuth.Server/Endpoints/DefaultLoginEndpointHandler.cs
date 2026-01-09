using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public sealed class DefaultLoginEndpointHandler<TUserId> : ILoginEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IUAuthFlowService<TUserId> _flow;
    private readonly IDeviceResolver _deviceResolver;
    private readonly IClock _clock;
    private readonly ICredentialResponseWriter _credentialResponseWriter;
    private readonly AuthRedirectResolver _redirectResolver;

    public DefaultLoginEndpointHandler(
        IAuthFlowContextAccessor authContext,
        IUAuthFlowService<TUserId> flow,
        IDeviceResolver deviceResolver,
        IClock clock,
        ICredentialResponseWriter credentialResponseWriter,
        AuthRedirectResolver redirectResolver)
    {
        _authContext = authContext;
        _flow = flow;
        _deviceResolver = deviceResolver;
        _clock = clock;
        _credentialResponseWriter = credentialResponseWriter;
        _redirectResolver = redirectResolver;
    }

    public async Task<IResult> LoginAsync(HttpContext ctx)
    {
        var auth = _authContext.Current;

        var shouldIssueTokens =
            auth.Response.AccessTokenDelivery.Mode != TokenResponseMode.None ||
            auth.Response.RefreshTokenDelivery.Mode != TokenResponseMode.None;

        if (!ctx.Request.HasFormContentType)
            return Results.BadRequest("Invalid content type.");

        var form = await ctx.Request.ReadFormAsync();

        var identifier = form["Identifier"].ToString();
        var secret = form["Secret"].ToString();

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(secret))
            return RedirectFailure(ctx, AuthFailureReason.InvalidCredentials, auth.OriginalOptions);

        var tenantCtx = ctx.GetTenantContext();

        var flowRequest = new LoginRequest
        {
            Identifier = identifier,
            Secret = secret,
            TenantId = tenantCtx.TenantId,
            At = _clock.UtcNow,
            DeviceInfo = _deviceResolver.Resolve(ctx),
            RequestTokens = shouldIssueTokens
        };

        var result = await _flow.LoginAsync(auth, flowRequest, ctx.RequestAborted);

        if (!result.IsSuccess)
            return RedirectFailure(ctx, result.FailureReason ?? AuthFailureReason.Unknown, auth.OriginalOptions);

        if (result.SessionId is not null)
        {
            _credentialResponseWriter.Write(ctx, CredentialKind.Session, result.SessionId.Value);
        }

        if (result.AccessToken is not null)
        {
            _credentialResponseWriter.Write(ctx, CredentialKind.AccessToken, result.AccessToken.Token);
        }

        if (result.RefreshToken is not null)
        {
            _credentialResponseWriter.Write(ctx, CredentialKind.RefreshToken, result.RefreshToken.Token);
        }

        if (auth.Response.Login.RedirectEnabled)
        {
            var redirectUrl = _redirectResolver.ResolveRedirect(ctx, auth.Response.Login.SuccessPath);
            return Results.Redirect(redirectUrl);
        }

        // PKCE / API login
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
