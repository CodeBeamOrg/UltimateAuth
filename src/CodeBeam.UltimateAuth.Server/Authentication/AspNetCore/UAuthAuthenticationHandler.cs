using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CodeBeam.UltimateAuth.Server.Authentication;

internal sealed class UAuthAuthenticationHandler : AuthenticationHandler<UAuthAuthenticationSchemeOptions>
{
    private readonly ITransportCredentialResolver _transportCredentialResolver;
    private readonly ISessionValidator _sessionValidator;
    private readonly IDeviceContextFactory _deviceContextFactory;
    private readonly IAuthStateSnapshotFactory _snapshotFactory;
    private readonly IUAuthJwtValidator _jwtValidator;
    private readonly IEffectiveServerOptionsProvider _effectiveOptions;
    private readonly IApplicationProfileReader _applicationProfileReader;
    private readonly IAuthResponseResolver _authResponseResolver;
    private readonly UAuthServerOptions _serverOptions;
    private readonly IClock _clock;

    public UAuthAuthenticationHandler(
        ITransportCredentialResolver transportCredentialResolver,
        IOptionsMonitor<UAuthAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISessionValidator sessionValidator,
        IDeviceContextFactory deviceContextFactory,
        IAuthStateSnapshotFactory snapshotFactory,
        IUAuthJwtValidator jwtValidator,
        IEffectiveServerOptionsProvider effectiveOptions,
        IApplicationProfileReader applicationProfileReader,
        IAuthResponseResolver authResponseResolver,
        IOptions<UAuthServerOptions> serverOptions,
        IClock uauthClock)
        : base(options, logger, encoder)
    {
        _transportCredentialResolver = transportCredentialResolver;
        _sessionValidator = sessionValidator;
        _deviceContextFactory = deviceContextFactory;
        _snapshotFactory = snapshotFactory;
        _jwtValidator = jwtValidator;
        _effectiveOptions = effectiveOptions;
        _applicationProfileReader = applicationProfileReader;
        _authResponseResolver = authResponseResolver;
        _serverOptions = serverOptions.Value;
        _clock = uauthClock;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var applicationProfile = await _applicationProfileReader.ReadAsync(Context);
        var tenant = Context.GetTenant();
        var effective = _effectiveOptions.GetEffective(tenant, applicationProfile);

        var mode = effective.Mode;

        var response = _authResponseResolver.ResolveForAuthentication(mode, applicationProfile, effective);

        var credential = await _transportCredentialResolver.ResolveAsync(Context, mode, response);

        if (mode == UAuthMode.PureJwt)
        {
            if (credential is null)
                return AuthenticateResult.NoResult();

            if (credential.Kind != TransportCredentialKind.AccessToken)
                return AuthenticateResult.Fail("PureJwt requires an access token.");

            return await AuthenticateJwtAsync(credential.Value, tenant, effective.Options.Session.DeviceMismatchBehavior);
        }

        if (credential is null)
            return AuthenticateResult.NoResult();

        if (!AuthSessionId.TryCreate(credential.Value, out var sessionId))
            return AuthenticateResult.Fail("Invalid credential");

        var result = await _sessionValidator.ValidateSessionAsync(
            new SessionValidationContext
            {
                Tenant = tenant,
                SessionId = sessionId,
                Device = _deviceContextFactory.Create(credential.Device),
                SessionOptions = _serverOptions.Session,
                Now = _clock.UtcNow
            });

        if (!result.IsValid || result.UserKey is null)
            return AuthenticateResult.NoResult();

        var snapshot = await _snapshotFactory.CreateAsync(result);

        if (snapshot is null || snapshot.Identity is null)
            return AuthenticateResult.NoResult();

        RateLimiting.UAuthRateLimitPolicy.SetValidatedActor(Context, tenant.Value, result.UserKey.Value.Value);

        var principal = snapshot.ToClaimsPrincipal(UAuthConstants.SchemeDefaults.GlobalScheme);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, UAuthConstants.SchemeDefaults.GlobalScheme));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (!_serverOptions.Navigation.EnableAutomaticNavigationRedirect)
        {
            Context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        bool isNavigation = Context.Request.Headers["sec-fetch-mode"] == "navigate" &&
            Context.Request.Headers["Accept"].Any(a => a?.Contains("text/html") == true) && 
            !Context.Request.Headers.ContainsKey("X-Requested-With");

        if (isNavigation)
        {
            var resolver = _serverOptions.Navigation.LoginResolver ?? (_ => UAuthConstants.Routes.LoginRedirect);

            var loginPath = resolver(Context);
            var returnUrl = Context.Request.Path + Context.Request.QueryString;
            var redirectUrl = $"{loginPath}?{UAuthConstants.Query.ReturnUrl}={Uri.EscapeDataString(returnUrl)}";

            Context.Response.Redirect(redirectUrl);
            return Task.CompletedTask;
        }

        // fetch request → pure 401
        Context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        Context.Response.Headers[UAuthConstants.Headers.AuthState] = "unauthenticated";
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        if (!_serverOptions.Navigation.EnableAutomaticNavigationRedirect)
        {
            Context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        bool isNavigation = Context.Request.Headers["sec-fetch-mode"] == "navigate" &&
            Context.Request.Headers["Accept"].Any(a => a?.Contains("text/html") == true) &&
            !Context.Request.Headers.ContainsKey("X-Requested-With");

        if (isNavigation)
        {
            var resolver = _serverOptions.Navigation.AccessDeniedResolver ?? (_ => UAuthConstants.Routes.LoginRedirect);
            var path = resolver(Context);
            Context.Response.Redirect(path);
            return Task.CompletedTask;
        }

        Context.Response.StatusCode = StatusCodes.Status403Forbidden;
        Context.Response.Headers[UAuthConstants.Headers.AuthState] = "forbidden";
        return Task.CompletedTask;
    }

    private async Task<AuthenticateResult> AuthenticateJwtAsync(string token, TenantKey tenant, DeviceMismatchBehavior mismatchBehavior)
    {
        var validated = await _jwtValidator.ValidateAsync(token, tenant, Context.RequestAborted);

        if (validated is null)
            return AuthenticateResult.Fail("Invalid JWT access token.");

        // PureJwt credentials must not carry framework session identity.
        if (validated.Claims.Any(x => x.Type == "sid"))
        {
            return AuthenticateResult.Fail("Session-bound JWT is not valid in PureJwt mode.");
        }

        var deviceClaims = validated.Claims
            .Where(x => x.Type == "device_id")
            .ToArray();

        if (deviceClaims.Length != 1 || !DeviceId.TryCreate(deviceClaims[0].Value, out var tokenDeviceId))
        {
            return AuthenticateResult.Fail("PureJwt requires a valid device binding.");
        }

        if (mismatchBehavior == DeviceMismatchBehavior.AllowAndRebind)
        {
            return AuthenticateResult.Fail("Device rebinding is not supported in PureJwt.");
        }

        if (mismatchBehavior is not (DeviceMismatchBehavior.Reject or DeviceMismatchBehavior.Allow))
        {
            return AuthenticateResult.Fail("Unsupported device mismatch behavior.");
        }

        DeviceId? requestDeviceId = null;

        if (mismatchBehavior == DeviceMismatchBehavior.Reject)
        {
            var requestDevice = await Context.GetDeviceAsync();

            if (DeviceId.TryCreate(requestDevice.DeviceId.Value, out var parsedDeviceId))
            {
                requestDeviceId = parsedDeviceId;
            }
        }

        if (!PureJwtDevicePolicy.IsAllowed(mismatchBehavior, tokenDeviceId, requestDeviceId))
        {
            return AuthenticateResult.Fail("JWT device mismatch.");
        }

        var claims = validated.Claims.ToList();

        // Use the cryptographically validated subject, not an untrusted request header or client-provided identifier.
        claims.RemoveAll(x => x.Type == ClaimTypes.NameIdentifier);

        claims.Add(new Claim(ClaimTypes.NameIdentifier, validated.Subject));

        var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.NameIdentifier, ClaimTypes.Role);

        var principal = new ClaimsPrincipal(identity);

        RateLimiting.UAuthRateLimitPolicy.SetValidatedActor(Context, tenant.Value, validated.Subject);

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
