using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceProvider _services;
    private readonly UAuthResourceApiOptions _resourceOptions;
    private readonly IClock _clock;

    public UAuthResourceAuthenticationHandler(
        ITransportCredentialResolver credentialResolver,
        ISessionValidator sessionValidator,
        IDeviceContextFactory deviceFactory,
        IServiceProvider services,
        IOptions<UAuthResourceApiOptions> resourceOptions,
        IClock clock,
        IOptionsMonitor<UAuthAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _credentialResolver = credentialResolver;
        _sessionValidator = sessionValidator;
        _deviceFactory = deviceFactory;
        _services = services;
        _resourceOptions = resourceOptions.Value;
        _clock = clock;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tenant = Context.GetTenant();

        if (_resourceOptions.AuthMode == UAuthMode.PureJwt)
            return await AuthenticatePureJwtAsync(tenant);

        return await AuthenticateSessionAsync(tenant);
    }

    private async Task<AuthenticateResult> AuthenticateSessionAsync(TenantKey tenant)
    {
        var mode = _resourceOptions.AuthMode;

        var sessionDelivery = new EffectiveGrantDelivery(
            GrantKind.Session,
            TokenFormat.Opaque,
            _resourceOptions.SessionDeliveryMode,
            name: "Authorization",
            headerFormat: HeaderTokenFormat.Bearer,
            cookie: new UAuthCookieOptions
            {
                Name = _resourceOptions.SessionCookieName
            });

        var response = new EffectiveAuthResponse(
            sessionDelivery,
            EffectiveGrantDelivery.Disabled(GrantKind.AccessToken),
            EffectiveGrantDelivery.Disabled(GrantKind.RefreshToken),
            EffectiveRedirectResponse.Disabled);

        var credential = await _credentialResolver.ResolveAsync(Context, mode, response);

        if (credential is null)
            return AuthenticateResult.NoResult();

        if (credential.Kind != TransportCredentialKind.Session)
        {
            return AuthenticateResult.Fail("Resource authentication requires a session credential.");
        }

        if (!AuthSessionId.TryCreate(credential.Value, out var sessionId))
            return AuthenticateResult.Fail("Invalid session");

        var result = await _sessionValidator.ValidateSessionAsync(
            new SessionValidationContext
            {
                Tenant = tenant,
                SessionId = sessionId,
                Device = _deviceFactory.Create(credential.Device),
                Now = _clock.UtcNow
            },
            Context.RequestAborted);

        if (!result.IsValid || result.UserKey is null)
            return AuthenticateResult.NoResult();

        Context.Items[UAuthConstants.HttpItems.SessionValidationResult] = result;

        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.UserKey.Value)
    };

        foreach (var (type, values) in result.Claims.Claims)
        {
            foreach (var value in values)
                claims.Add(new Claim(type, value));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    private async Task<AuthenticateResult> AuthenticatePureJwtAsync(CodeBeam.UltimateAuth.Core.MultiTenancy.TenantKey tenant)
    {
        var accessDelivery = new EffectiveGrantDelivery(
        GrantKind.AccessToken,
        TokenFormat.Jwt,
        _resourceOptions.AccessTokenDeliveryMode,
        name: _resourceOptions.AccessTokenHeaderName,
        headerFormat: _resourceOptions.AccessTokenHeaderFormat,
        cookie: new UAuthCookieOptions
        {
            Name = _resourceOptions.AccessTokenCookieName
        });

        var response = new EffectiveAuthResponse(
            EffectiveGrantDelivery.Disabled(GrantKind.Session),
            accessDelivery,
            EffectiveGrantDelivery.Disabled(GrantKind.RefreshToken),
            EffectiveRedirectResponse.Disabled);

        var credential = await _credentialResolver.ResolveAsync(
            Context,
            UAuthMode.PureJwt,
            response);

        if (credential is null)
            return AuthenticateResult.NoResult();

        if (credential.Kind != TransportCredentialKind.AccessToken)
            return AuthenticateResult.Fail("PureJwt requires an access token.");

        var validator = _services.GetRequiredService<IUAuthJwtValidator>();

        var validated = await validator.ValidateAsync(
            credential.Value,
            tenant,
            Context.RequestAborted);

        if (validated is null)
            return AuthenticateResult.Fail("Invalid JWT access token.");

        // PureJwt must never carry a framework session identity.
        if (validated.Claims.Any(x => x.Type == "sid"))
        {
            return AuthenticateResult.Fail("Session-bound JWT is not valid in PureJwt mode.");
        }

        var deviceClaims = validated.Claims.Where(x => x.Type == "device_id").ToArray();

        if (deviceClaims.Length != 1 || !DeviceId.TryCreate(deviceClaims[0].Value, out var tokenDeviceId))
        {
            return AuthenticateResult.Fail("PureJwt requires a valid device binding.");
        }

        var requestDevice = await Context.GetDeviceAsync();

        if (!DeviceId.TryCreate(requestDevice.DeviceId.Value, out var requestDeviceId) || requestDeviceId != tokenDeviceId)
        {
            return AuthenticateResult.Fail("JWT device mismatch.");
        }

        var claims = validated.Claims.ToList();

        // The identity must come from the validated JWT subject.
        claims.RemoveAll(x => x.Type == ClaimTypes.NameIdentifier);

        claims.Add(new Claim(ClaimTypes.NameIdentifier, validated.Subject));

        var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.NameIdentifier, ClaimTypes.Role);

        var principal = new ClaimsPrincipal(identity);

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
