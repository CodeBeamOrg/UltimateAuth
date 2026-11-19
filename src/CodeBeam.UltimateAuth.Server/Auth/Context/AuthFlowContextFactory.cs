using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class AuthFlowContextFactory : IAuthFlowContextFactory
{
    private readonly IApplicationProfileReader _clientProfileReader;
    private readonly IPrimaryTokenResolver _primaryTokenResolver;
    private readonly IEffectiveServerOptionsProvider _serverOptionsProvider;
    private readonly IAuthResponseResolver _authResponseResolver;
    private readonly IDeviceResolver _deviceResolver;
    private readonly IDeviceContextFactory _deviceContextFactory;
    private readonly ISessionValidator _sessionValidator;
    private readonly IClock _clock;

    public AuthFlowContextFactory(
        IApplicationProfileReader clientProfileReader,
        IPrimaryTokenResolver primaryTokenResolver,
        IEffectiveServerOptionsProvider serverOptionsProvider,
        IAuthResponseResolver authResponseResolver,
        IDeviceResolver deviceResolver,
        IDeviceContextFactory deviceContextFactory,
        ISessionValidator sessionValidator,
        IClock clock)
    {
        _clientProfileReader = clientProfileReader;
        _primaryTokenResolver = primaryTokenResolver;
        _serverOptionsProvider = serverOptionsProvider;
        _authResponseResolver = authResponseResolver;
        _deviceResolver = deviceResolver;
        _deviceContextFactory = deviceContextFactory;
        _sessionValidator = sessionValidator;
        _clock = clock;
    }

    public async ValueTask<AuthFlowContext> CreateAsync(HttpContext ctx, AuthFlowType flowType, CancellationToken ct = default)
    {
        var tenant = ctx.GetTenant();
        var sessionCtx = ctx.GetSessionContext();
        var user = ctx.GetUserContext();

        var clientProfile = await _clientProfileReader.ReadAsync(ctx);

        var effective = _serverOptionsProvider.GetEffective(ctx, clientProfile);
        var options = effective.Options;

        var effectiveMode = effective.Mode;
        var allowedModes = options.AllowedModes;

        if (allowedModes is { Count: > 0 } && !allowedModes.Contains(effectiveMode))
            throw new InvalidOperationException($"Auth mode '{effectiveMode}' is not allowed by server configuration.");

        var primaryTokenKind = _primaryTokenResolver.Resolve(effectiveMode);
        var response = _authResponseResolver.Resolve(effectiveMode, flowType, clientProfile, effective);
        var deviceInfo = await _deviceResolver.ResolveAsync(ctx);
        var deviceContext = _deviceContextFactory.Create(deviceInfo);
        var returnUrl = await ctx.GetReturnUrlAsync();
        var returnUrlInfo = ReturnUrlParser.Parse(returnUrl);


        SessionSecurityContext? sessionSecurityContext = null;
        bool isAuthenticated;
        UserKey? userKey;

        if (effectiveMode == UAuthMode.PureJwt)
        {
            // The UserMiddleware has already populated this snapshot from the cryptographically authenticated JWT principal.
            // No session validation is performed for PureJwt.
            isAuthenticated = user?.IsAuthenticated == true;
            userKey = isAuthenticated ? user?.UserId : null;
        }
        else
        {
            isAuthenticated = user?.IsAuthenticated ?? false;
            userKey = user?.UserId;

            if (effectiveMode is UAuthMode.PureOpaque or UAuthMode.Hybrid)
            {
                if (!sessionCtx.IsAnonymous)
                {
                    var validation = await _sessionValidator.ValidateSessionAsync(
                        new SessionValidationContext
                        {
                            Tenant = tenant,
                            SessionId = sessionCtx.SessionId!.Value,
                            Device = deviceContext,
                            SessionOptions = options.Session,
                            Now = _clock.UtcNow
                        },
                        ct);

                    sessionSecurityContext = SessionValidationMapper.ToSecurityContext(validation);

                    isAuthenticated = validation.IsValid;

                    if (!isAuthenticated)
                        userKey = null;
                }
                else
                {
                    isAuthenticated = false;
                    userKey = null;
                }
            }
        }

        if (!tenant.IsValid || tenant.IsUnresolved)
            throw new InvalidOperationException("AuthFlowContext cannot be created with invalid or unresolved tenant.");

        // TODO: Implement invariant checker
        //_invariantChecker.Validate(flowType, effectiveMode, response, effectiveOptions);

        return new AuthFlowContext(
            flowType,
            clientProfile,
            effectiveMode,
            deviceContext,
            tenant,
            isAuthenticated,
            userKey,
            sessionSecurityContext,
            options,
            response,
            primaryTokenKind,
            returnUrlInfo
        );
    }

    public async ValueTask<AuthFlowContext> RecreateWithClientProfileAsync(AuthFlowContext existing, UAuthApplicationProfile overriddenProfile, CancellationToken ct = default)
    {
        var flowType = existing.FlowType;
        var tenant = existing.Tenant;

        var effective = _serverOptionsProvider.GetEffective(tenant, overriddenProfile);
        var options = effective.Options;
        var effectiveMode = effective.Mode;

        var allowedModes = options.AllowedModes;

        if (allowedModes is { Count: > 0 } && !allowedModes.Contains(effectiveMode))
        {
            throw new InvalidOperationException($"Auth mode '{effectiveMode}' is not allowed by server configuration.");
        }

        if (effectiveMode != existing.EffectiveMode && existing.IsAuthenticated)
        {
            throw new InvalidOperationException("An authenticated flow cannot change its authentication mode through a client profile override.");
        }

        var primaryTokenKind = _primaryTokenResolver.Resolve(effectiveMode);
        var response = _authResponseResolver.Resolve(effectiveMode, flowType, overriddenProfile, effective);

        var returnUrlInfo = existing.ReturnUrlInfo;
        var deviceContext = existing.Device;
        var session = existing.Session;

        return new AuthFlowContext(
            flowType,
            overriddenProfile,
            effectiveMode,
            deviceContext,
            tenant,
            existing.IsAuthenticated,
            existing.UserKey,
            session,
            options,
            response,
            primaryTokenKind,
            returnUrlInfo
        );
    }

    public ValueTask<AuthFlowContext> RecreateWithDeviceAsync(AuthFlowContext existing, DeviceContext device, CancellationToken ct = default)
    {
        var flowType = existing.FlowType;
        var tenant = existing.Tenant;

        return ValueTask.FromResult(new AuthFlowContext(
            flowType,
            existing.ClientProfile,
            existing.EffectiveMode,
            device,
            tenant,
            existing.IsAuthenticated,
            existing.UserKey,
            existing.Session,
            existing.Options,
            existing.Response,
            existing.PrimaryTokenKind,
            existing.ReturnUrlInfo
        ));
    }
}
