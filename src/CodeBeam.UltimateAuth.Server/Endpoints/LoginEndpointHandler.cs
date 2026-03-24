using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class LoginEndpointHandler : ILoginEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IUAuthInternalFlowService _internalFlowService;
    private readonly ICredentialResponseWriter _credentialResponseWriter;
    private readonly IAuthRedirectResolver _redirectResolver;
    private readonly IAuthStore _authStore;
    private readonly ILoginIdentifierResolver _loginIdentifierResolver;
    private readonly UAuthServerOptions _options;
    private readonly IClock _clock;

    public LoginEndpointHandler(
        IAuthFlowContextAccessor authFlow,
        IUAuthInternalFlowService internalFlowService,
        ICredentialResponseWriter credentialResponseWriter,
        IAuthRedirectResolver redirectResolver,
        IAuthStore authStore,
        ILoginIdentifierResolver loginIdentifierResolver,
        IOptions<UAuthServerOptions> options,
        IClock clock)
    {
        _authFlow = authFlow;
        _internalFlowService = internalFlowService;
        _credentialResponseWriter = credentialResponseWriter;
        _redirectResolver = redirectResolver;
        _authStore = authStore;
        _loginIdentifierResolver = loginIdentifierResolver;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<IResult> LoginAsync(HttpContext ctx)
    {
        var authFlow = _authFlow.Current;
        var request = await ReadLoginRequestAsync(ctx);

        if (request is null || string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Secret))
        {
            var failure = _redirectResolver.ResolveFailure(authFlow, ctx, AuthFailureReason.InvalidCredentials);
            return failure.Enabled ? Results.Redirect(failure.TargetUrl!) : Results.Unauthorized();
        }

        var suppressFailureAttempt = false;

        if (!string.IsNullOrWhiteSpace(request.PreviewReceipt) && authFlow.Device.DeviceId is DeviceId deviceId)
        {
            var key = new AuthArtifactKey(request.PreviewReceipt);
            var artifact = await _authStore.GetAsync(key, ctx.RequestAborted) as LoginPreviewArtifact;

            if (artifact is not null)
            {
                var fingerprint = LoginPreviewFingerprint.Create(
                    authFlow.Tenant,
                    request.Identifier,
                    CredentialType.Password,
                    request.Secret,
                    deviceId);

                if (artifact.Matches(
                    authFlow.Tenant,
                    artifact.UserKey,
                    CredentialType.Password,
                    deviceId.Value,
                    request.Identifier,
                    authFlow.ClientProfile,
                    fingerprint))
                {
                    suppressFailureAttempt = true;
                    await _authStore.ConsumeAsync(key, ctx.RequestAborted);
                }
            }
        }

        var flowRequest = new LoginRequest
        {
            Identifier = request.Identifier,
            Secret = request.Secret,
            Factor = CredentialType.Password,
            PreviewReceipt = request.PreviewReceipt,
            RequestTokens = authFlow.AllowsTokenIssuance,
            Metadata = request.Metadata,
        };

        var result = await _internalFlowService.LoginAsync(authFlow, flowRequest,
            new LoginExecutionOptions
            {
                Mode = LoginExecutionMode.Commit,
                SuppressFailureAttempt = suppressFailureAttempt,
                SuppressSuccessReset = false
            }, ctx.RequestAborted);

        if (!result.IsSuccess)
        {
            var decisionFailure = _redirectResolver.ResolveFailure(authFlow, ctx, result.FailureReason ?? AuthFailureReason.Unknown, result);

            return decisionFailure.Enabled
                ? Results.Redirect(decisionFailure.TargetUrl!)
                : Results.Unauthorized();
        }

        if (result.SessionId is AuthSessionId sessionId)
        {
            _credentialResponseWriter.Write(ctx, GrantKind.Session, sessionId);
        }

        if (result.AccessToken is not null)
        {
            _credentialResponseWriter.Write(ctx, GrantKind.AccessToken, result.AccessToken);
        }

        if (result.RefreshToken is not null)
        {
            _credentialResponseWriter.Write(ctx, GrantKind.RefreshToken, result.RefreshToken);
        }

        var decision = _redirectResolver.ResolveSuccess(authFlow, ctx);

        return decision.Enabled
            ? Results.Redirect(decision.TargetUrl!)
            : Results.Ok();
    }

    public async Task<IResult> TryLoginAsync(HttpContext ctx)
    {
        var authFlow = _authFlow.Current;

        if (!ctx.Request.HasFormContentType && !ctx.Request.HasJsonContentType())
            return Results.BadRequest("Invalid content type.");

        var request = await ReadLoginRequestAsync(ctx);
        if (request is null)
            return Results.BadRequest("Invalid payload.");

        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Secret))
        {
            return Results.Ok(new TryLoginResult
            {
                Success = false,
                Reason = AuthFailureReason.InvalidCredentials
            });
        }

        request = new LoginRequest
        {
            Identifier = request.Identifier,
            Secret = request.Secret,
            Factor = CredentialType.Password,
            PreviewReceipt = request.PreviewReceipt,
            RequestTokens = authFlow.AllowsTokenIssuance,
            Metadata = request.Metadata
        };

        var result = await _internalFlowService.LoginAsync(
            authFlow,
            request,
            new LoginExecutionOptions
            {
                Mode = LoginExecutionMode.Preview,
                SuppressFailureAttempt = false,
                SuppressSuccessReset = true
            },
            ctx.RequestAborted);

        string? previewReceipt = null;

        if (result.IsSuccess && authFlow.Device.DeviceId is DeviceId deviceId)
        {
            var fingerprint = LoginPreviewFingerprint.Create(
                authFlow.Tenant,
                request.Identifier,
                request.Factor,
                request.Secret,
                deviceId);

            var receipt = AuthArtifactKey.New();
            previewReceipt = receipt.Value;

            var resolution = await _loginIdentifierResolver.ResolveAsync(authFlow.Tenant, request.Identifier, ctx.RequestAborted);

            if (resolution?.UserKey is { } userKey)
            {
                var artifact = new LoginPreviewArtifact(
                    authFlow.Tenant,
                    userKey,
                    request.Factor,
                    deviceId.Value,
                    request.Identifier,
                    authFlow.ClientProfile,
                    fingerprint,
                    _clock.UtcNow.Add(_options.Login.TryLoginDuration));

                await _authStore.StoreAsync(receipt, artifact, ctx.RequestAborted);
            }
        }

        return Results.Ok(new TryLoginResult
        {
            Success = result.IsSuccess,
            Reason = result.FailureReason,
            RemainingAttempts = result.RemainingAttempts,
            LockoutUntilUtc = result.LockoutUntilUtc,
            RequiresMfa = result.RequiresMfa,
            PreviewReceipt = previewReceipt
        });
    }

    private async Task<LoginRequest?> ReadLoginRequestAsync(HttpContext ctx)
    {
        if (ctx.Request.HasJsonContentType())
        {
            return await ctx.Request.ReadFromJsonAsync<LoginRequest>(cancellationToken: ctx.RequestAborted);
        }

        if (ctx.Request.HasFormContentType)
        {
            var form = await ctx.GetCachedFormAsync();

            return new LoginRequest
            {
                Identifier = form?["Identifier"].FirstOrDefault() ?? string.Empty,
                Secret = form?["Secret"].FirstOrDefault() ?? string.Empty,
                PreviewReceipt = form?["PreviewReceipt"].FirstOrDefault(),
                // TODO: Other properties?
            };
        }

        return null;
    }
}
