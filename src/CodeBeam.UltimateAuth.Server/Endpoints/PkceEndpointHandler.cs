using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class PkceEndpointHandler : IPkceEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IUAuthFlowService _flow;
    private readonly IAuthStore _authStore;
    private readonly IPkceAuthorizationValidator _validator;
    private readonly IClock _clock;
    private readonly UAuthServerOptions _options;
    private readonly ICredentialResponseWriter _credentialResponseWriter;
    private readonly IAuthRedirectResolver _redirectResolver;

    public PkceEndpointHandler(
        IAuthFlowContextAccessor authContext,
        IUAuthFlowService flow,
        IAuthStore authStore,
        IPkceAuthorizationValidator validator,
        IClock clock,
        IOptions<UAuthServerOptions> options,
        ICredentialResponseWriter credentialResponseWriter,
        IAuthRedirectResolver redirectResolver)
    {
        _authContext = authContext;
        _flow = flow;
        _authStore = authStore;
        _validator = validator;
        _clock = clock;
        _options = options.Value;
        _credentialResponseWriter = credentialResponseWriter;
        _redirectResolver = redirectResolver;
    }

    public async Task<IResult> AuthorizeAsync(HttpContext ctx)
    {
        var authContext = _authContext.Current;

        // TODO: Make PKCE flow free
        if (authContext.FlowType != AuthFlowType.Login)
            return Results.BadRequest("PKCE is only supported for login flow.");

        var request = await ReadPkceAuthorizeRequestAsync(ctx);
        if (request is null)
            return Results.BadRequest("Invalid content type.");

        if (string.IsNullOrWhiteSpace(request.CodeChallenge))
            return Results.BadRequest("code_challenge is required.");

        if (!string.Equals(request.ChallengeMethod, "S256", StringComparison.Ordinal))
            return Results.BadRequest("Only S256 challenge method is supported.");

        var authorizationCode = AuthArtifactKey.New();

        var snapshot = new PkceContextSnapshot(
            clientProfile: authContext.ClientProfile,
            tenant: authContext.Tenant,
            redirectUri: request.RedirectUri,
            deviceId: request.DeviceId
        );

        var expiresAt = _clock.UtcNow.AddSeconds(_options.Pkce.AuthorizationCodeLifetimeSeconds);

        var artifact = new PkceAuthorizationArtifact(
            authorizationCode: authorizationCode,
            codeChallenge: request.CodeChallenge,
            challengeMethod: PkceChallengeMethod.S256,
            expiresAt: expiresAt,
            context: snapshot
        );

        await _authStore.StoreAsync(authorizationCode, artifact, ctx.RequestAborted);

        return Results.Ok(new PkceAuthorizeResponse
        {
            AuthorizationCode = authorizationCode.Value,
            ExpiresIn = _options.Pkce.AuthorizationCodeLifetimeSeconds
        });
    }

    public async Task<IResult> CompleteAsync(HttpContext ctx)
    {
        var authContext = _authContext.Current;

        if (authContext.FlowType != AuthFlowType.Login)
            return Results.BadRequest("PKCE is only supported for login flow.");

        var request = await ReadPkceCompleteRequestAsync(ctx);
        if (request is null)
            return Results.BadRequest("Invalid PKCE completion payload.");

        if (string.IsNullOrWhiteSpace(request.AuthorizationCode) || string.IsNullOrWhiteSpace(request.CodeVerifier))
            return Results.BadRequest("authorization_code and code_verifier are required.");

        var artifactKey = new AuthArtifactKey(request.AuthorizationCode);
        var artifact = await _authStore.ConsumeAsync(artifactKey, ctx.RequestAborted) as PkceAuthorizationArtifact;

        if (artifact is null)
            return Results.Unauthorized(); // replay / expired / unknown code

        var validation = _validator.Validate(artifact, request.CodeVerifier,
            new PkceContextSnapshot(
                clientProfile: authContext.ClientProfile,
                tenant: authContext.Tenant,
                redirectUri: null,
                deviceId: artifact.Context.DeviceId),
            _clock.UtcNow);

        if (!validation.Success)
        {
            artifact.RegisterAttempt();
            return await RedirectToLoginWithErrorAsync(ctx, authContext, "invalid");
        }

        var loginRequest = new LoginRequest
        {
            Identifier = request.Identifier,
            Secret = request.Secret,
            Tenant = authContext.Tenant,
            At = _clock.UtcNow,
            RequestTokens = authContext.AllowsTokenIssuance
        };

        var execution = new AuthExecutionContext
        {
            EffectiveClientProfile = artifact.Context.ClientProfile,
            Device = DeviceContext.Create(DeviceId.Create(artifact.Context.DeviceId), null, null, null, null, null)
        };

        var result = await _flow.LoginAsync(authContext, execution, loginRequest, ctx.RequestAborted);

        if (!result.IsSuccess)
            return await RedirectToLoginWithErrorAsync(ctx, authContext, "invalid");

        if (result.SessionId is not null)
        {
            _credentialResponseWriter.Write(ctx, GrantKind.Session, result.SessionId.Value);
        }

        if (result.AccessToken is not null)
        {
            _credentialResponseWriter.Write(ctx, GrantKind.AccessToken, result.AccessToken);
        }

        if (result.RefreshToken is not null)
        {
            _credentialResponseWriter.Write(ctx, GrantKind.RefreshToken, result.RefreshToken);
        }

        var decision = _redirectResolver.ResolveSuccess(authContext, ctx);

        return decision.Enabled
            ? Results.Redirect(decision.TargetUrl!)
            : Results.Ok();
    }

    private static async Task<PkceAuthorizeRequest?> ReadPkceAuthorizeRequestAsync(HttpContext ctx)
    {
        if (ctx.Request.HasJsonContentType())
        {
            return await ctx.Request.ReadFromJsonAsync<PkceAuthorizeRequest>(cancellationToken: ctx.RequestAborted);
        }

        if (ctx.Request.HasFormContentType)
        {
            var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);

            var codeChallenge = form["code_challenge"].ToString();
            var challengeMethod = form["challenge_method"].ToString();
            var redirectUri = form["redirect_uri"].ToString();
            var deviceId = form["device_id"].ToString();

            return new PkceAuthorizeRequest
            {
                CodeChallenge = codeChallenge,
                ChallengeMethod = challengeMethod,
                RedirectUri = string.IsNullOrWhiteSpace(redirectUri) ? null : redirectUri,
                DeviceId = deviceId
            };
        }

        return null;
    }

    private static async Task<PkceCompleteRequest?> ReadPkceCompleteRequestAsync(HttpContext ctx)
    {
        if (ctx.Request.HasJsonContentType())
        {
            return await ctx.Request.ReadFromJsonAsync<PkceCompleteRequest>(
                cancellationToken: ctx.RequestAborted);
        }

        if (ctx.Request.HasFormContentType)
        {
            var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);

            var authorizationCode = form["authorization_code"].ToString();
            var codeVerifier = form["code_verifier"].ToString();
            var identifier = form["Identifier"].ToString();
            var secret = form["Secret"].ToString();
            var returnUrl = form["return_url"].ToString();

            return new PkceCompleteRequest
            {
                AuthorizationCode = authorizationCode,
                CodeVerifier = codeVerifier,
                Identifier = identifier,
                Secret = secret,
                ReturnUrl = returnUrl
            };
        }

        return null;
    }

    private async Task<IResult> RedirectToLoginWithErrorAsync(HttpContext ctx, AuthFlowContext auth, string error)
    {
        var basePath = auth.OriginalOptions.Hub.LoginPath ?? "/login";
        var hubKey = ctx.Request.Query["hub"].ToString();

        if (!string.IsNullOrWhiteSpace(hubKey))
        {
            var key = new AuthArtifactKey(hubKey);
            var artifact = await _authStore.GetAsync(key, ctx.RequestAborted);

            if (artifact is HubFlowArtifact hub)
            {
                hub.MarkCompleted();
                await _authStore.StoreAsync(key, hub, ctx.RequestAborted);
            }

            return Results.Redirect(
                $"{basePath}?hub={Uri.EscapeDataString(hubKey)}&__uauth_error={Uri.EscapeDataString(error)}");
        }

        return Results.Redirect(
            $"{basePath}?__uauth_error={Uri.EscapeDataString(error)}");
    }
}
