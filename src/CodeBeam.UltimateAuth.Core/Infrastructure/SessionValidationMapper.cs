using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public static class SessionValidationMapper
{
    public static SessionValidationResult ToDomain(AuthValidationResult dto, AuthSessionId sessionId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!dto.IsValid)
        {
            return SessionValidationResult.Invalid(
                dto.State,
                sessionId: sessionId,
                chainId: dto.ChainId,
                rootId: dto.RootId,
                boundDeviceId: dto.BoundDeviceId);
        }

        // An active validation result must contain
        // the required identity and security lineage.

        if (dto.Snapshot?.Identity is null)
        {
            return SessionValidationResult.Invalid(SessionState.Invalid, sessionId: sessionId);
        }

        if (dto.ChainId is not SessionChainId chainId || chainId.IsUnassigned)
        {
            return SessionValidationResult.Invalid(SessionState.Invalid, sessionId: sessionId);
        }

        if (dto.RootId is not SessionRootId rootId || rootId.Value == Guid.Empty)
        {
            return SessionValidationResult.Invalid(SessionState.Invalid, sessionId: sessionId, chainId: chainId);
        }

        var identity = dto.Snapshot.Identity;

        return SessionValidationResult.Active(
            tenant: identity.Tenant,
            userKey: identity.UserKey,
            sessionId: sessionId,
            chainId: chainId,
            rootId: rootId,
            claims: dto.Snapshot.Claims,
            authenticatedAt: identity.AuthenticatedAt ?? DateTimeOffset.UtcNow,
            boundDeviceId: dto.BoundDeviceId);
    }

    public static SessionSecurityContext? ToSecurityContext(SessionValidationResult result)
    {
        if (result.SessionId is null)
            return null;

        return new SessionSecurityContext
        {
            SessionId = result.SessionId.Value,
            State = result.State,
            ChainId = result.ChainId,
            UserKey = result.UserKey,
            BoundDeviceId = result.BoundDeviceId
        };
    }
}
