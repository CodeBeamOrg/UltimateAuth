using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Constants;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace CodeBeam.UltimateAuth.Server.Authentication;

internal sealed class UAuthAuthenticationHandler : AuthenticationHandler<UAuthAuthenticationSchemeOptions>
{
    private readonly ITransportCredentialResolver _transportCredentialResolver;
    private readonly ISessionValidator _sessionValidator;
    private readonly IDeviceContextFactory _deviceContextFactory;
    private readonly IAuthStateSnapshotFactory _snapshotFactory;
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
        IOptions<UAuthServerOptions> serverOptions,
        IClock uauthClock)
        : base(options, logger, encoder)
    {
        _transportCredentialResolver = transportCredentialResolver;
        _sessionValidator = sessionValidator;
        _deviceContextFactory = deviceContextFactory;
        _snapshotFactory = snapshotFactory;
        _serverOptions = serverOptions.Value;
        _clock = uauthClock;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var credential = _transportCredentialResolver.Resolve(Context);

        if (credential is null)
            return AuthenticateResult.NoResult();

        if (!AuthSessionId.TryCreate(credential.Value, out var sessionId))
            return AuthenticateResult.Fail("Invalid credential");

        var tenant = Context.GetTenant();

        var result = await _sessionValidator.ValidateSessionAsync(
            new SessionValidationContext
            {
                Tenant = tenant,
                SessionId = sessionId,
                Device = _deviceContextFactory.Create(credential.Device),
                Now = _clock.UtcNow
            });

        if (!result.IsValid || result.UserKey is null)
            return AuthenticateResult.NoResult();

        var snapshot = await _snapshotFactory.CreateAsync(result);

        if (snapshot is null || snapshot.Identity is null)
            return AuthenticateResult.NoResult();

        var principal = snapshot.ToClaimsPrincipal(UAuthSchemeDefaults.AuthenticationScheme);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, UAuthSchemeDefaults.AuthenticationScheme));
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
}