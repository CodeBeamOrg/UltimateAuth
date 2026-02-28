using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CodeBeam.UltimateAuth.Server.Services;

internal sealed class UAuthSessionValidator : ISessionValidator
{
    private readonly ISessionStoreFactory _storeFactory;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly UAuthServerOptions _options;

    public UAuthSessionValidator(
        ISessionStoreFactory storeFactory,
        IUserClaimsProvider claimsProvider,
        IOptions<UAuthServerOptions> options)
    {
        _storeFactory = storeFactory;
        _claimsProvider = claimsProvider;
        _options = options.Value;
    }

    // TODO: Improve Device binding
    // Validate runs before AuthFlowContext is set, do not call _authFlow here.
    public async Task<SessionValidationResult> ValidateSessionAsync(SessionValidationContext context, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(context.Tenant);
        var session = await kernel.GetSessionAsync(context.SessionId);

        if (session is null)
            return SessionValidationResult.Invalid(SessionState.NotFound, sessionId: context.SessionId);

        var state = session.GetState(context.Now);
        if (state != SessionState.Active)
            return SessionValidationResult.Invalid(state, session.UserKey, session.SessionId, session.ChainId);

        var chain = await kernel.GetChainAsync(session.ChainId);
        if (chain is null || chain.IsRevoked)
            return SessionValidationResult.Invalid(SessionState.Revoked, session.UserKey, session.SessionId, session.ChainId);

        var chainState = chain.GetState(context.Now, _options.Session.IdleTimeout);
        if (chainState != SessionState.Active)
            return SessionValidationResult.Invalid(chainState, chain.UserKey, session.SessionId, chain.ChainId);

        //if (chain.ActiveSessionId != session.SessionId)
        //    return SessionValidationResult.Invalid(SessionState.SecurityMismatch, chain.UserKey, session.SessionId, chain.ChainId);

        if (chain.ChainId != session.ChainId)
            return SessionValidationResult.Invalid(SessionState.SecurityMismatch, chain.UserKey, session.SessionId, chain.ChainId);

        if (chain.Tenant != context.Tenant)
            return SessionValidationResult.Invalid(SessionState.SecurityMismatch, chain.UserKey, session.SessionId, chain.ChainId);

        var root = await kernel.GetRootByUserAsync(session.UserKey);
        if (root is null || root.IsRevoked)
            return SessionValidationResult.Invalid(SessionState.Revoked, chain.UserKey, session.SessionId, chain.ChainId, root?.RootId);

        if (chain.RootId != root.RootId)
            return SessionValidationResult.Invalid(SessionState.SecurityMismatch, chain.UserKey, session.SessionId, chain.ChainId, root.RootId);

        if (session.SecurityVersionAtCreation != root.SecurityVersion)
            return SessionValidationResult.Invalid(SessionState.SecurityMismatch, session.UserKey, session.SessionId, session.ChainId, root.RootId);

        if (chain.Device.HasDeviceId && context.Device.HasDeviceId)
        {
            if (!Equals(chain.Device.DeviceId, context.Device.DeviceId))
            {
                if (_options.Session.DeviceMismatchBehavior == DeviceMismatchBehavior.Reject)
                    return SessionValidationResult.Invalid(SessionState.DeviceMismatch, chain.UserKey, session.SessionId, chain.ChainId, root.RootId);

                //if (_options.Session.DeviceMismatchBehavior == AllowAndRebind)
            }
        }
        //else
        //{
        //    // Add SessionValidatorOrigin to seperate UserRequest or background task
        //    return SessionValidationResult.Invalid(SessionState.DeviceMismatch, chain.UserKey, session.SessionId, chain.ChainId, root.RootId);
        //}

        var claims = await _claimsProvider.GetClaimsAsync(context.Tenant, session.UserKey, ct);
        return SessionValidationResult.Active(context.Tenant, session.UserKey, session.SessionId, session.ChainId, root.RootId, claims, session.CreatedAt, chain.Device.DeviceId);
    }
}
