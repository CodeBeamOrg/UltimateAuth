using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public sealed class LoginEndpointHandler<TUserId> : ILoginEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IUAuthFlowService<TUserId> _flowService;
    private readonly IClock _clock;
    private readonly ICredentialResponseWriter _credentialResponseWriter;
    private readonly IAuthRedirectResolver _redirectResolver;

    public LoginEndpointHandler(
        IAuthFlowContextAccessor authFlow,
        IUAuthFlowService<TUserId> flowService,
        IClock clock,
        ICredentialResponseWriter credentialResponseWriter,
        IAuthRedirectResolver redirectResolver)
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

        if (!ctx.Request.HasFormContentType)
            return Results.BadRequest("Invalid content type.");

        var form = await ctx.Request.ReadFormAsync();

        var identifier = form["Identifier"].ToString();
        var secret = form["Secret"].ToString();

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(secret))
        {
            var decisionFailureInvalid = _redirectResolver.ResolveFailure(authFlow, ctx, AuthFailureReason.InvalidCredentials);

            return decisionFailureInvalid.Enabled
                ? Results.Redirect(decisionFailureInvalid.TargetUrl!)
                : Results.Unauthorized();
        }

        var flowRequest = new LoginRequest
        {
            Identifier = identifier,
            Secret = secret,
            Tenant = authFlow.Tenant,
            At = _clock.UtcNow,
            Device = authFlow.Device,
            RequestTokens = authFlow.AllowsTokenIssuance
        };

        var result = await _flowService.LoginAsync(authFlow, flowRequest, ctx.RequestAborted);

        if (!result.IsSuccess)
        {
            var decisionFailure = _redirectResolver.ResolveFailure(authFlow, ctx, result.FailureReason ?? AuthFailureReason.Unknown, result);

            return decisionFailure.Enabled
                ? Results.Redirect(decisionFailure.TargetUrl!)
                : Results.Unauthorized();
        }

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

        var decision = _redirectResolver.ResolveSuccess(authFlow, ctx);

        return decision.Enabled
            ? Results.Redirect(decision.TargetUrl!)
            : Results.Ok();
    }
}
