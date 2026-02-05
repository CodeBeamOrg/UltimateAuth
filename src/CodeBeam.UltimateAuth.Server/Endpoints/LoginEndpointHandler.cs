using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public sealed class LoginEndpointHandler<TUserId> : ILoginEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IUAuthFlowService<TUserId> _flowService;
    private readonly IClock _clock;
    private readonly ICredentialResponseWriter _credentialResponseWriter;
    private readonly AuthRedirectResolver _redirectResolver;

    public LoginEndpointHandler(
        IAuthFlowContextAccessor authFlow,
        IUAuthFlowService<TUserId> flowService,
        IClock clock,
        ICredentialResponseWriter credentialResponseWriter,
        AuthRedirectResolver redirectResolver)
    {
        _authFlow = authFlow;
        _flowService = flowService;
        _clock = clock;
        _credentialResponseWriter = credentialResponseWriter;
        _redirectResolver = redirectResolver;
    }

    public async Task<IResult> LoginAsync(HttpContext ctx)
    {
        var authFlow = _authFlow.Current;

        var shouldIssueTokens =
            authFlow.Response.AccessTokenDelivery.Mode != TokenResponseMode.None ||
            authFlow.Response.RefreshTokenDelivery.Mode != TokenResponseMode.None;

        if (!ctx.Request.HasFormContentType)
            return Results.BadRequest("Invalid content type.");

        var form = await ctx.Request.ReadFormAsync();

        var identifier = form["Identifier"].ToString();
        var secret = form["Secret"].ToString();

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(secret))
            return RedirectFailure(ctx, AuthFailureReason.InvalidCredentials, authFlow.OriginalOptions);

        var flowRequest = new LoginRequest
        {
            Identifier = identifier,
            Secret = secret,
            Tenant = authFlow.Tenant,
            At = _clock.UtcNow,
            Device = authFlow.Device,
            RequestTokens = shouldIssueTokens
        };

        var result = await _flowService.LoginAsync(authFlow, flowRequest, ctx.RequestAborted);

        if (!result.IsSuccess)
            return RedirectFailure(ctx, result.FailureReason ?? AuthFailureReason.Unknown, authFlow.OriginalOptions);

        if (result.SessionId is AuthSessionId sessionId)
        {
            _credentialResponseWriter.Write(ctx, CredentialKind.Session, sessionId);
        }

        if (result.AccessToken is not null)
        {
            _credentialResponseWriter.Write(ctx, CredentialKind.AccessToken, result.AccessToken);
        }

        if (result.RefreshToken is not null)
        {
            _credentialResponseWriter.Write(ctx, CredentialKind.RefreshToken, result.RefreshToken);
        }

        if (authFlow.Response.Login.RedirectEnabled)
        {
            var redirectUrl = _redirectResolver.ResolveRedirect(ctx, authFlow.Response.Login.SuccessPath);
            return Results.Redirect(redirectUrl);
        }

        return Results.Ok();
    }

    private IResult RedirectFailure(HttpContext ctx, AuthFailureReason reason, UAuthServerOptions options)
    {
        var login = options.AuthResponse.Login;

        var code = login.FailureCodes != null &&
            login.FailureCodes.TryGetValue(reason, out var mapped)
                ? mapped
                : "failed";

        var redirectUrl = _redirectResolver.ResolveRedirect(ctx, login.FailureRedirect,
            new Dictionary<string, string?>
            {
                [login.FailureQueryKey] = code
            });

        return Results.Redirect(redirectUrl);
    }
}
