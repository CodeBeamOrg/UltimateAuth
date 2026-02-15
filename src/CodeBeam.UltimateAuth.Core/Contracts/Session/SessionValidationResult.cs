using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class SessionValidationResult
{
    public TenantKey Tenant { get; init; }

    public required SessionState State { get; init; }

    public UserKey? UserKey { get; init; }

    public AuthSessionId? SessionId { get; init; }

    public SessionChainId? ChainId { get; init; }

    public SessionRootId? RootId { get; init; }

    public DeviceId? BoundDeviceId { get; init; }

    public DateTimeOffset? AuthenticatedAt { get; init; }

    public ClaimsSnapshot Claims { get; init; } = ClaimsSnapshot.Empty;

    public bool IsValid => State == SessionState.Active;

    private SessionValidationResult() { }

    public static SessionValidationResult Active(
        TenantKey tenant,
        UserKey? userId,
        AuthSessionId sessionId,
        SessionChainId chainId,
        SessionRootId rootId,
        ClaimsSnapshot claims,
        DateTimeOffset authenticatedAt,
        DeviceId? boundDeviceId = null)
        => new()
        {
            Tenant = tenant,
            State = SessionState.Active,
            UserKey = userId,
            SessionId = sessionId,
            ChainId = chainId,
            RootId = rootId,
            Claims = claims,
            AuthenticatedAt = authenticatedAt,
            BoundDeviceId = boundDeviceId
        };

    public static SessionValidationResult Invalid(
        SessionState state,
        UserKey? userId = null,
        AuthSessionId? sessionId = null,
        SessionChainId? chainId = null,
        SessionRootId? rootId = null,
        DeviceId? boundDeviceId = null)
    => new()
    {
        State = state,
        UserKey = userId,
        SessionId = sessionId,
        ChainId = chainId,
        RootId = rootId,
        Claims = ClaimsSnapshot.Empty,
        BoundDeviceId = boundDeviceId
    };
}
