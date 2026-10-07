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
                chainId: TryParseChainId(dto.ChainId),
                rootId: TryParseRootId(dto.RootId),
                boundDeviceId: TryParseDeviceId(dto.BoundDeviceId));
        }

        //
        // Active is a stronger contract than merely receiving
        // a successful HTTP response. All required security
        // lineage and identity data must be present.
        //

        if (dto.Snapshot?.Identity is null)
        {
            return SessionValidationResult.Invalid(
                SessionState.Invalid,
                sessionId: sessionId);
        }

        if (dto.ChainId is not Guid chainGuid ||
            chainGuid == Guid.Empty)
        {
            return SessionValidationResult.Invalid(
                SessionState.Invalid,
                sessionId: sessionId);
        }

        var chainId =
            SessionChainId.From(chainGuid);

        if (dto.RootId is not Guid rootGuid ||
            rootGuid == Guid.Empty)
        {
            return SessionValidationResult.Invalid(
                SessionState.Invalid,
                sessionId: sessionId,
                chainId: chainId);
        }

        var rootId =
            SessionRootId.From(rootGuid);

        var identity =
            dto.Snapshot.Identity;

        var boundDeviceId =
            TryParseDeviceId(dto.BoundDeviceId);

        return SessionValidationResult.Active(
            tenant: identity.Tenant,
            userKey: identity.UserKey,
            sessionId: sessionId,
            chainId: chainId,
            rootId: rootId,
            claims: dto.Snapshot.Claims,
            authenticatedAt:
                identity.AuthenticatedAt
                ?? DateTimeOffset.UtcNow,
            boundDeviceId: boundDeviceId);
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

    private static SessionChainId? TryParseChainId(Guid? value)
    {
        if (value is not Guid guid ||
            guid == Guid.Empty)
        {
            return null;
        }

        return SessionChainId.From(guid);
    }

    private static SessionRootId? TryParseRootId(Guid? value)
    {
        if (value is not Guid guid ||
            guid == Guid.Empty)
        {
            return null;
        }

        return SessionRootId.From(guid);
    }

    private static DeviceId? TryParseDeviceId(string? value)
    {
        return DeviceId.TryCreate(
            value,
            out var id)
            ? id
            : null;
    }
}
