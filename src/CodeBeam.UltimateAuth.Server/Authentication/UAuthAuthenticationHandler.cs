using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Authentication;

internal sealed class UAuthAuthenticationHandler : AuthenticationHandler<UAuthAuthenticationCookieOptions>
{
    private readonly ITransportCredentialResolver _transportCredentialResolver;
    private readonly ISessionQueryService _sessionQuery;
    private readonly IDeviceContextFactory _deviceContextFactory;
    private readonly IClock _clock;

    public UAuthAuthenticationHandler(
        ITransportCredentialResolver transportCredentialResolver,
        IOptionsMonitor<UAuthAuthenticationCookieOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock,
        ISessionQueryService sessionQuery,
        IDeviceContextFactory deviceContextFactory,
        IClock uauthClock)
        : base(options, logger, encoder, clock)
    {
        _transportCredentialResolver = transportCredentialResolver;
        _sessionQuery = sessionQuery;
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

        var result = await _sessionQuery.ValidateSessionAsync(
            new SessionValidationContext
            {
                TenantId = credential.TenantId,
                SessionId = sessionId,
                Device = _deviceContextFactory.Create(credential.Device),
                Now = _clock.UtcNow
            });

        if (!result.IsValid || result.UserKey is null)
            return AuthenticateResult.NoResult();

        var principal = CreatePrincipal(result);
        var ticket = new AuthenticationTicket(principal,UAuthCookieDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }

    private static ClaimsPrincipal CreatePrincipal(SessionValidationResult result)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserKey.Value),
            new Claim("uauth:session_id", result.SessionId.ToString())
        };

        if (!string.IsNullOrEmpty(result.TenantId))
        {
            claims.Add(new Claim("uauth:tenant", result.TenantId));
        }

        // Session claims (snapshot)
        foreach (var (key, value) in result.Claims.AsDictionary())
        {
            claims.Add(new Claim(key, value));
        }

        var identity = new ClaimsIdentity(claims, UAuthCookieDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

}