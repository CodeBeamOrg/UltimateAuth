using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

public sealed class SessionProjection
{
    public long Id { get; set; } // EF internal PK

    public AuthSessionId SessionId { get; set; } = default!;
    public SessionChainId ChainId { get; set; } = default!;

    public TenantKey Tenant { get; set; }
    public UserKey UserKey { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    
    public DateTimeOffset? RevokedAt { get; set; }

    public long SecurityVersionAtCreation { get; set; }

    public DeviceContext Device { get; set; } = DeviceContext.Anonymous();
    public ClaimsSnapshot Claims { get; set; } = ClaimsSnapshot.Empty;
    public SessionMetadata Metadata { get; set; } = SessionMetadata.Empty;

    public long Version { get; set; }

    public bool IsRevoked => RevokedAt != null;
}
