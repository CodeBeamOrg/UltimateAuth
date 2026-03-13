using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal sealed class SessionRootProjection
{
    public long Id { get; set; }
    public SessionRootId RootId { get; set; }
    public TenantKey Tenant { get; set; }
    public UserKey UserKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public long SecurityVersion { get; set; }
    public long Version { get; set; }
}
