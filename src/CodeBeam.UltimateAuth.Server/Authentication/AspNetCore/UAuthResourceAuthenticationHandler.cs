using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CodeBeam.UltimateAuth.Server.Authentication;

internal sealed class UAuthResourceAuthenticationHandler : AuthenticationHandler<UAuthAuthenticationSchemeOptions>
{
    private readonly ITransportCredentialResolver _credentialResolver;
    private readonly ISessionValidator _sessionValidator;
    private readonly IDeviceContextFactory _deviceFactory;
    private readonly IClock _clock;

    public UAuthResourceAuthenticationHandler(
        ITransportCredentialResolver credentialResolver,
        ISessionValidator sessionValidator,
        IDeviceContextFactory deviceFactory,
        IClock clock,
        IOptionsMonitor<UAuthAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _credentialResolver = credentialResolver;
        _sessionValidator = sessionValidator;
        _deviceFactory = deviceFactory;
        _clock = clock;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var credential = await _credentialResolver.ResolveAsync(Context);

        if (credential is null)
            return AuthenticateResult.NoResult();

        if (!AuthSessionId.TryCreate(credential.Value, out var sessionId))
            return AuthenticateResult.Fail("Invalid session");

        var tenant = Context.GetTenant();

        var result = await _sessionValidator.ValidateSessionAsync(new SessionValidationContext
        {
            Tenant = tenant,
            SessionId = sessionId,
            Device = _deviceFactory.Create(credential.Device),
            Now = _clock.UtcNow
        });

        if (!result.IsValid || result.UserKey is null)
            return AuthenticateResult.NoResult();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserKey.Value)
        };

        foreach (var (type, values) in result.Claims.Claims)
        {
            foreach (var value in values)
            {
                claims.Add(new Claim(type, value));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
