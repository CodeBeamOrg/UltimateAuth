using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

internal sealed class PkceEndpointHandler : IPkceEndpointHandler
{
    private readonly IAuthFlowContextAccessor _authContext;
    private readonly IUAuthFlowService _flow;
    private readonly IPkceService _pkceService;
    private readonly IUAuthInternalFlowService _internalFlowService;
    private readonly IAuthStore _authStore;
    private readonly IPkceAuthorizationValidator _validator;
    private readonly IClock _clock;
    private readonly ICredentialResponseWriter _credentialResponseWriter;
    private readonly IAuthRedirectResolver _redirectResolver;

    public PkceEndpointHandler(
        IAuthFlowContextAccessor authContext,
        IUAuthFlowService flow,
        IPkceService pkceService,
        IUAuthInternalFlowService internalFlowService,
        IAuthStore authStore,
        IPkceAuthorizationValidator validator,
        IClock clock,
        ICredentialResponseWriter credentialResponseWriter,
        IAuthRedirectResolver redirectResolver)
    {
        _authContext = authContext;
        _flow = flow;
        _pkceService = pkceService;
        _internalFlowService = internalFlowService;
        _authStore = authStore;
        _validator = validator;
        _clock = clock;
        _credentialResponseWriter = credentialResponseWriter;
        _redirectResolver = redirectResolver;
    }

    public async Task<IResult> AuthorizeAsync(HttpContext ctx)
    {
        var auth = _authContext.Current;

        var request = await ReadPkceAuthorizeRequestAsync(ctx);
        if (request is null)
            return Results.BadRequest("Invalid content type.");

        var result = await _pkceService.AuthorizeAsync(
            new PkceAuthorizeCommand
            {
                CodeChallenge = request.CodeChallenge,
                ChallengeMethod = request.ChallengeMethod,
                Device = request.Device,
                RedirectUri = request.RedirectUri,
                ClientProfile = auth.ClientProfile,
                Tenant = auth.Tenant
            },
            ctx.RequestAborted);

        return Results.Ok(new PkceAuthorizeResponse
        {
            AuthorizationCode = result.AuthorizationCode,
            ExpiresIn = result.ExpiresIn
        });
    }

    public async Task<IResult> TryCompleteAsync(HttpContext ctx)
    {
        var authContext = _authContext.Current;

        if (authContext.FlowType != AuthFlowType.Login)
            return Results.BadRequest("PKCE is only supported for login flow.");

        var request = await ReadPkceCompleteRequestAsync(ctx);
        if (request is null)
            return Results.BadRequest("Invalid PKCE payload.");

        if (string.IsNullOrWhiteSpace(request.AuthorizationCode) || string.IsNullOrWhiteSpace(request.CodeVerifier))
            return Results.BadRequest("authorization_code and code_verifier are required.");

        var artifactKey = new AuthArtifactKey(request.AuthorizationCode);
        var artifact = await _authStore.GetAsync(artifactKey, ctx.RequestAborted) as PkceAuthorizationArtifact;

        if (artifact is null)
        {
            return Results.Ok(new TryPkceLoginResult
            {
                Success = false,
                RetryWithNewPkce = true
            });
        }

        var validation = _validator.Validate(
            artifact,
            request.CodeVerifier,
            new PkceContextSnapshot(
                clientProfile: authContext.ClientProfile,
                tenant: authContext.Tenant,
                redirectUri: null,
                device: artifact.Context.Device),
            _clock.UtcNow);

        if (!validation.Success)
        {
            return Results.Ok(new TryPkceLoginResult
            {
                Success = false,
                RetryWithNewPkce = true
            });
        }

        var loginRequest = new LoginRequest
        {
            Identifier = request.Identifier,
            Secret = request.Secret,
            RequestTokens = authContext.AllowsTokenIssuance
        };

        var execution = new AuthExecutionContext
        {
            EffectiveClientProfile = artifact.Context.ClientProfile,
            Device = artifact.Context.Device
        };

        var preview = await _internalFlowService.LoginAsync(authContext, execution, loginRequest,
             new LoginExecutionOptions
             {
                 Mode = LoginExecutionMode.Preview,
                 SuppressFailureAttempt = false,
                 SuppressSuccessReset = true
             }, ctx.RequestAborted);

        return Results.Ok(new TryPkceLoginResult
        {
            Success = preview.IsSuccess,
            Reason = preview.FailureReason,
            RemainingAttempts = preview.RemainingAttempts,
            LockoutUntilUtc = preview.LockoutUntilUtc,
            RequiresMfa = preview.FailureReason == AuthFailureReason.RequiresMfa,
            RetryWithNewPkce = false
        });
    }

    public async Task<IResult> CompleteAsync(HttpContext ctx)
    {
        var auth = _authContext.Current;

        var request = await ReadPkceCompleteRequestAsync(ctx);
        if (request is null)
            return Results.BadRequest("Invalid PKCE payload.");

        var result = await _pkceService.CompleteAsync(
            auth,
            new PkceCompleteRequest
            {
                AuthorizationCode = request.AuthorizationCode!,
                CodeVerifier = request.CodeVerifier!,
                Identifier = request.Identifier,
                Secret = request.Secret
            },
            ctx.RequestAborted);

        if (result.InvalidPkce)
            return Results.Unauthorized();

        if (!result.Success)
            return await RedirectToLoginWithErrorAsync(ctx, auth, "invalid");

        var login = result.LoginResult!;

        if (login.SessionId is not null)
            _credentialResponseWriter.Write(ctx, GrantKind.Session, login.SessionId.Value);

        if (login.AccessToken is not null)
            _credentialResponseWriter.Write(ctx, GrantKind.AccessToken, login.AccessToken);

        if (login.RefreshToken is not null)
            _credentialResponseWriter.Write(ctx, GrantKind.RefreshToken, login.RefreshToken);

        var decision = _redirectResolver.ResolveSuccess(auth, ctx);

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
            var form = await ctx.GetCachedFormAsync();

            var codeChallenge = form["code_challenge"].ToString();
            var challengeMethod = form["challenge_method"].ToString();
            var redirectUri = form["redirect_uri"].ToString();

            var deviceRaw = form["device"].FirstOrDefault();
            DeviceContext? device = null;

            if (!string.IsNullOrWhiteSpace(deviceRaw))
            {
                try
                {
                    var bytes = WebEncoders.Base64UrlDecode(deviceRaw);
                    var json = Encoding.UTF8.GetString(bytes);
                    device = JsonSerializer.Deserialize<DeviceContext>(json);
                }
                catch
                {
                    device = null;
                }
            }

            if (device == null)
            {
                var info = await ctx.GetDeviceAsync();
                device = DeviceContext.Create(info.DeviceId, info.DeviceType, info.Platform, info.OperatingSystem, info.Browser, info.IpAddress);
            }

            return new PkceAuthorizeRequest
            {
                CodeChallenge = codeChallenge,
                ChallengeMethod = challengeMethod,
                RedirectUri = string.IsNullOrWhiteSpace(redirectUri) ? null : redirectUri,
                Device = device
            };
        }

        return null;
    }

    private static async Task<PkceCompleteRequest?> ReadPkceCompleteRequestAsync(HttpContext ctx)
    {
        if (ctx.Request.HasJsonContentType())
        {
            return await ctx.Request.ReadFromJsonAsync<PkceCompleteRequest>(cancellationToken: ctx.RequestAborted);
        }

        if (ctx.Request.HasFormContentType)
        {
            var form = await ctx.GetCachedFormAsync();

            var authorizationCode = form["authorization_code"].FirstOrDefault();
            var codeVerifier = form["code_verifier"].FirstOrDefault();
            var identifier = form["Identifier"].FirstOrDefault();
            var secret = form["Secret"].FirstOrDefault();
            var returnUrl = form["return_url"].FirstOrDefault();

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
        var hubKey = await ResolveHubKeyAsync(ctx);

        if (!string.IsNullOrWhiteSpace(hubKey))
        {
            var key = new AuthArtifactKey(hubKey);
            var artifact = await _authStore.GetAsync(key, ctx.RequestAborted);

            if (artifact is HubFlowArtifact hub)
            {
                hub.SetError(HubErrorCode.InvalidCredentials);
                await _authStore.StoreAsync(key, hub);

                return Results.Redirect($"{basePath}?{UAuthConstants.Query.Hub}={Uri.EscapeDataString(hubKey)}");
            }
        }
        return Results.Redirect(basePath);
    }

    private async Task<string?> ResolveHubKeyAsync(HttpContext ctx)
    {
        if (ctx.Request.Query.TryGetValue(UAuthConstants.Query.Hub, out var q) && !string.IsNullOrWhiteSpace(q))
            return q.ToString();

        if (ctx.Request.HasFormContentType)
        {
            var form = await ctx.GetCachedFormAsync();

            if (form?.TryGetValue("hub_session_id", out var f) == true && !string.IsNullOrWhiteSpace(f))
                return f.ToString();
        }

        return null;
    }
}
