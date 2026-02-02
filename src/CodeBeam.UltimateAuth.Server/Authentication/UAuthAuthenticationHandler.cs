using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Authentication;

internal sealed class UAuthAuthenticationHandler : AuthenticationHandler<UAuthAuthenticationCookieOptions>
{
    private readonly ITransportCredentialResolver _transportCredentialResolver;
    private readonly ISessionValidator _sessionValidator;
    private readonly IDeviceContextFactory _deviceContextFactory;
    private readonly IClock _clock;

    public UAuthAuthenticationHandler(
        ITransportCredentialResolver transportCredentialResolver,
        IOptionsMonitor<UAuthAuthenticationCookieOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock,
        ISessionValidator sessionValidator,
        IDeviceContextFactory deviceContextFactory,
        IClock uauthClock)
        : base(options, logger, encoder, clock)
    {
        _transportCredentialResolver = transportCredentialResolver;
        _sessionValidator = sessionValidator;
        _deviceContextFactory = deviceContextFactory;
        _clock = uauthClock;
    }
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var credential = _transportCredentialResolver.Resolve(Context);

        if (credential is null)
            return AuthenticateResult.NoResult();

        if (!AuthSessionId.TryCreate(credential.Value, out var sessionId))
            return AuthenticateResult.Fail("Invalid credential");

        var result = await _sessionValidator.ValidateSessionAsync(
            new SessionValidationContext
            {
                TenantId = credential.TenantId,
                SessionId = sessionId,
                Device = _deviceContextFactory.Create(credential.Device),
                Now = _clock.UtcNow
            });

        if (!result.IsValid || result.UserKey is null)
            return AuthenticateResult.NoResult();

        var principal = result.Claims.ToClaimsPrincipal(UAuthCookieDefaults.AuthenticationScheme);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, UAuthCookieDefaults.AuthenticationScheme));
    }
}