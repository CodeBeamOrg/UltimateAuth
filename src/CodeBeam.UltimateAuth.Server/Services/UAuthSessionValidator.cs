using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Services;

internal sealed class UAuthSessionValidator : ISessionValidator
{
    private readonly ISessionStoreKernelFactory _storeFactory;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly UAuthServerOptions _options;

    public UAuthSessionValidator(
        ISessionStoreKernelFactory storeFactory,
        IUserClaimsProvider claimsProvider,
        IOptions<UAuthServerOptions> options)
    {
        _storeFactory = storeFactory;
        _claimsProvider = claimsProvider;
        _options = options.Value;
    }

    // Validate runs before AuthFlowContext is set, do not call _authFlow here.
    public async Task<SessionValidationResult> ValidateSessionAsync(SessionValidationContext context, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(context.TenantId);
        var session = await kernel.GetSessionAsync(context.SessionId);

        if (session is null)
            return SessionValidationResult.Invalid(SessionState.NotFound, sessionId: context.SessionId);

        var state = session.GetState(context.Now, _options.Session.IdleTimeout);
        if (state != SessionState.Active)
            return SessionValidationResult.Invalid(state, sessionId: session.SessionId, chainId: session.ChainId);

        var chain = await kernel.GetChainAsync(session.ChainId);
        if (chain is null || chain.IsRevoked)
            return SessionValidationResult.Invalid(SessionState.Revoked, session.UserKey, session.SessionId, session.ChainId);

        var root = await kernel.GetSessionRootByUserAsync(session.UserKey);
        if (root is null || root.IsRevoked)
            return SessionValidationResult.Invalid(SessionState.Revoked, session.UserKey, session.SessionId, session.ChainId, root?.RootId);

        if (session.SecurityVersionAtCreation != root.SecurityVersion)
            return SessionValidationResult.Invalid(SessionState.SecurityMismatch, session.UserKey, session.SessionId, session.ChainId, root.RootId);

        // TODO: Implement device id, AllowAndRebind behavior and check device mathing in blazor server circuit and external http calls.
        // Currently this line has error on refresh flow.
        //if (!session.Device.Matches(context.Device) && _options.Session.DeviceMismatchBehavior == DeviceMismatchBehavior.Reject)
        //    return SessionValidationResult<TUserId>.Invalid(SessionState.DeviceMismatch);

        var claims = await _claimsProvider.GetClaimsAsync(context.TenantId, session.UserKey, ct);
        return SessionValidationResult.Active(context.TenantId, session.UserKey, session.SessionId, session.ChainId, root.RootId, claims, boundDeviceId: session.Device.DeviceId);
    }
}
