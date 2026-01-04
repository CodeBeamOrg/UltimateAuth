using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Authentication;

internal sealed class UAuthAuthenticationHandler
    : AuthenticationHandler<UAuthAuthenticationCookieOptions>
{
    private readonly ICredentialResolver _credentialResolver;
    private readonly ISessionQueryService<UserId> _sessionQuery;
    private readonly IAuthFlowContextFactory _flowFactory;
    private readonly IAuthResponseResolver _responseResolver;

    private readonly IClock _clock;

    public UAuthAuthenticationHandler(
        IOptionsMonitor<UAuthAuthenticationCookieOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock,
        ICredentialResolver credentialResolver,
        ISessionQueryService<UserId> sessionQuery,
        IAuthFlowContextFactory flowFactory,
        IAuthResponseResolver responseResolver,
        IClock uauthClock)
        : base(options, logger, encoder, clock)
    {
        _credentialResolver = credentialResolver;
        _sessionQuery = sessionQuery;
        _flowFactory = flowFactory;
        _responseResolver = responseResolver;
        _clock = uauthClock;
    }
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1️⃣ Credential al
        var flow = _flowFactory.Create(Context, AuthFlowType.Api);
        var response = _responseResolver.Resolve(flow);
        var credential = _credentialResolver.Resolve(Context, response);

        if (credential is null)
            return AuthenticateResult.NoResult();

        // 2️⃣ SessionId parse et
        if (!AuthSessionId.TryCreate(credential.Value, out var sessionId))
            return AuthenticateResult.Fail("Invalid session id");

        // 3️⃣ Session validate et
        var result = await _sessionQuery.ValidateSessionAsync(
            new SessionValidationContext
            {
                TenantId = credential.TenantId,
                SessionId = sessionId,
                Device = credential.Device,
                Now = _clock.UtcNow
            });

        if (!result.IsValid)
            return AuthenticateResult.NoResult();

        // 4️⃣ Claims üret
        var principal = CreatePrincipal(result);

        // 5️⃣ Ticket üret
        var ticket = new AuthenticationTicket(
            principal,
            UAuthCookieDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }

    private static ClaimsPrincipal CreatePrincipal(SessionValidationResult<UserId> result)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.Session.UserId.Value),
            new Claim("uauth:session_id", result.Session.SessionId.Value)
        };

        if (!string.IsNullOrEmpty(result.TenantId))
        {
            claims.Add(new Claim("uauth:tenant", result.TenantId));
        }

        // Session claims (snapshot)
        foreach (var (key, value) in result.Session.Claims.AsDictionary())
        {
            claims.Add(new Claim(key, value));
        }

        var identity = new ClaimsIdentity(
            claims,
            UAuthCookieDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }

}