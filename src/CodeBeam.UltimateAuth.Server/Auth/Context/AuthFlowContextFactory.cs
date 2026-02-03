using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
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

        var clientProfile = _clientProfileReader.Read(ctx);
        var originalOptions = _serverOptionsProvider.GetOriginal(ctx);
        var effectiveOptions = _serverOptionsProvider.GetEffective(ctx, flowType, clientProfile);

        var effectiveMode = effectiveOptions.Mode;
        var primaryTokenKind = _primaryTokenResolver.Resolve(effectiveMode);

        var response = _authResponseResolver.Resolve(effectiveMode, flowType, clientProfile, effectiveOptions);

        var deviceInfo = _deviceResolver.Resolve(ctx);
        var deviceContext = _deviceContextFactory.Create(deviceInfo);

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
            primaryTokenKind
        );
    }

}
