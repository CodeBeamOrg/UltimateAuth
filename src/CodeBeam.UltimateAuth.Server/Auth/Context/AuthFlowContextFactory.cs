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
    private readonly IClientProfileReader _clientProfileReader;
    private readonly IPrimaryTokenResolver _primaryTokenResolver;
    private readonly IEffectiveServerOptionsProvider _serverOptionsProvider;
    private readonly IAuthResponseResolver _authResponseResolver;
    private readonly IDeviceResolver _deviceResolver;
    private readonly IDeviceContextFactory _deviceContextFactory;
    private readonly ISessionValidator _sessionValidator;
    private readonly IClock _clock;

    public AuthFlowContextFactory(
        IClientProfileReader clientProfileReader,
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
        var originalOptions = _serverOptionsProvider.GetOriginal(ctx);
        var effectiveOptions = _serverOptionsProvider.GetEffective(ctx, flowType, clientProfile);

        var allowedModes = originalOptions.AllowedModes;

        if (allowedModes is { Count: > 0 } && !allowedModes.Contains(effectiveOptions.Mode))
        {
            throw new InvalidOperationException($"Auth mode '{effectiveOptions.Mode}' is not allowed by server configuration.");
        }

        var effectiveMode = effectiveOptions.Mode;
        var primaryTokenKind = _primaryTokenResolver.Resolve(effectiveMode);
        var response = _authResponseResolver.Resolve(effectiveMode, flowType, clientProfile, effectiveOptions);
        var deviceInfo = await _deviceResolver.ResolveAsync(ctx);
        var deviceContext = _deviceContextFactory.Create(deviceInfo);
        var returnUrl = await ctx.GetReturnUrlAsync();
        var returnUrlInfo = ReturnUrlParser.Parse(returnUrl);

        SessionSecurityContext? sessionSecurityContext = null;

        if (!sessionCtx.IsAnonymous)
        {
            var validation = await _sessionValidator.ValidateSessionAsync(
                new SessionValidationContext
                {
                    Tenant = tenant,
                    SessionId = sessionCtx.SessionId!.Value,
                    Device = deviceContext,
                    Now = _clock.UtcNow
                },
                ct);

            sessionSecurityContext = SessionValidationMapper.ToSecurityContext(validation);
        }

        if (tenant.IsUnresolved)
            throw new InvalidOperationException("AuthFlowContext cannot be created with unresolved tenant.");

        // TODO: Implement invariant checker
        //_invariantChecker.Validate(flowType, effectiveMode, response, effectiveOptions);

        return new AuthFlowContext(
            flowType,
            clientProfile,
            effectiveMode,
            deviceContext,
            tenant,
            user?.IsAuthenticated ?? false,
            user?.UserId,
            sessionSecurityContext,
            originalOptions,
            effectiveOptions,
            response,
            primaryTokenKind,
            returnUrlInfo
        );
    }

    public async ValueTask<AuthFlowContext> RecreateWithClientProfileAsync(AuthFlowContext existing, UAuthClientProfile overriddenProfile, CancellationToken ct = default)
    {
        var flowType = existing.FlowType;
        var tenant = existing.Tenant;

        var originalOptions = existing.OriginalOptions;
        var effectiveOptions = _serverOptionsProvider.GetEffective(tenant, flowType, overriddenProfile);

        var allowedModes = originalOptions.AllowedModes;

        if (allowedModes is { Count: > 0 } && !allowedModes.Contains(effectiveOptions.Mode))
        {
            throw new InvalidOperationException($"Auth mode '{effectiveOptions.Mode}' is not allowed by server configuration.");
        }

        var effectiveMode = effectiveOptions.Mode;
        var primaryTokenKind = _primaryTokenResolver.Resolve(effectiveMode);
        var response = _authResponseResolver.Resolve(effectiveMode, flowType, overriddenProfile, effectiveOptions);

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
            originalOptions,
            effectiveOptions,
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
            existing.OriginalOptions,
            existing.EffectiveOptions,
            existing.Response,
            existing.PrimaryTokenKind,
            existing.ReturnUrlInfo
        ));
    }
}
