using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

public sealed class SessionChainProjection
{
    public long Id { get; set; }

    public SessionChainId ChainId { get; set; } = default!;
    public SessionRootId RootId { get; set; }
    public TenantKey Tenant { get; set; }
    public UserKey UserKey { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? AbsoluteExpiresAt { get; set; }
    public DeviceId DeviceId { get; set; }
    public DeviceContext Device { get; set; } = DeviceContext.Anonymous();
    public ClaimsSnapshot ClaimsSnapshot { get; set; } = ClaimsSnapshot.Empty;
    public AuthSessionId? ActiveSessionId { get; set; }
    public int RotationCount { get; set; }
    public int TouchCount { get; set; }
    public long SecurityVersionAtCreation { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
    public long Version { get; set; }

    public bool IsRevoked => RevokedAt is not null;
    public SessionChainState State => IsRevoked ? SessionChainState.Revoked : ActiveSessionId is null ? SessionChainState.Passive : SessionChainState.Active;
}
