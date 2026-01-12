using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class DefaultPkceEndpointHandler<TUserId> : IPkceEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IUAuthFlowService<TUserId> _flow;
    private readonly IAuthStore _authStore;
    private readonly IPkceAuthorizationValidator _validator;
    private readonly IClock _clock;
    private readonly UAuthPkceOptions _pkceOptions;
    private readonly ICredentialResponseWriter _credentialResponseWriter;
    private readonly AuthRedirectResolver _redirectResolver;

    public DefaultPkceEndpointHandler(
        IAuthFlowContextAccessor authContext,
        IUAuthFlowService<TUserId> flow,
        IAuthStore authStore,
        IPkceAuthorizationValidator validator,
        IClock clock,
        IOptions<UAuthPkceOptions> pkceOptions,
        ICredentialResponseWriter credentialResponseWriter,
        AuthRedirectResolver redirectResolver)
    {
        _authContext = authContext;
        _flow = flow;
        _authStore = authStore;
        _validator = validator;
        _clock = clock;
        _pkceOptions = pkceOptions.Value;
        _credentialResponseWriter = credentialResponseWriter;
        _redirectResolver = redirectResolver;
    }

    public async Task<IResult> AuthorizeAsync(HttpContext ctx)
    {
        var auth = _authContext.Current;

        if (auth.FlowType != AuthFlowType.Login)
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
            clientProfile: auth.ClientProfile,
            tenantId: auth.TenantId,
            redirectUri: request.RedirectUri,
            deviceId: string.Empty // TODO: Fix here with device binding
        );

        var expiresAt = _clock.UtcNow.AddSeconds(_pkceOptions.AuthorizationCodeLifetimeSeconds);

        var artifact = new PkceAuthorizationArtifact(
            authorizationCode: authorizationCode,
            codeChallenge: request.CodeChallenge,
            challengeMethod: PkceChallengeMethod.S256,
            expiresAt: expiresAt,
            maxAttempts: _pkceOptions.MaxVerificationAttempts,
            context: snapshot
        );

        await _authStore.StoreAsync(authorizationCode, artifact, ctx.RequestAborted);

        return Results.Ok(new PkceAuthorizeResponse
        {
            AuthorizationCode = authorizationCode.Value,
            ExpiresIn = _pkceOptions.AuthorizationCodeLifetimeSeconds
        });
    }

    public async Task<IResult> CompleteAsync(HttpContext ctx)
    {
        var auth = _authContext.Current;

        if (auth.FlowType != AuthFlowType.Login)
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
                clientProfile: auth.ClientProfile,
                tenantId: auth.TenantId,
                redirectUri: null,
                deviceId: string.Empty),
            _clock.UtcNow);

        if (!validation.Success)
        {
            artifact.RegisterAttempt();
            return RedirectToLoginWithError(ctx, auth, "invalid");
        }

        var loginRequest = new LoginRequest
        {
            Identifier = request.Identifier,
            Secret = request.Secret,
            TenantId = auth.TenantId,
            At = _clock.UtcNow,
            DeviceInfo = DeviceInfo.Unknown, // TODO: Device binding will add
            RequestTokens = auth.AllowsTokenIssuance
        };

        var execution = new AuthExecutionContext
        {
            EffectiveClientProfile = artifact.Context.ClientProfile,
        };

        var result = await _flow.LoginAsync(auth, execution, loginRequest, ctx.RequestAborted);

        if (!result.IsSuccess)
            return RedirectToLoginWithError(ctx, auth, "invalid");

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
            var redirectUrl = request.ReturnUrl ?? _redirectResolver.ResolveRedirect(ctx, auth.Response.Login.SuccessPath);
            return Results.Redirect(redirectUrl);
        }

        return Results.Ok();
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

            return new PkceAuthorizeRequest
            {
                CodeChallenge = codeChallenge,
                ChallengeMethod = challengeMethod,
                RedirectUri = string.IsNullOrWhiteSpace(redirectUri) ? null : redirectUri
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

    private IResult RedirectToLoginWithError(HttpContext ctx, AuthFlowContext auth, string error)
    {
        var basePath = auth.OriginalOptions.Hub.LoginPath ?? "/login";

        var hubKey = ctx.Request.Query["hub"].ToString();

        if (!string.IsNullOrWhiteSpace(hubKey))
        {
            var key = new AuthArtifactKey(hubKey);
            var artifact = _authStore.GetAsync(key, ctx.RequestAborted).Result;

            if (artifact is HubFlowArtifact hub)
            {
                hub.MarkCompleted();
                _authStore.StoreAsync(key, hub, ctx.RequestAborted);
            }
            return Results.Redirect($"{basePath}?hub={Uri.EscapeDataString(hubKey)}&__uauth_error={Uri.EscapeDataString(error)}");
        }

        return Results.Redirect($"{basePath}?__uauth_error={Uri.EscapeDataString(error)}");
    }

}
