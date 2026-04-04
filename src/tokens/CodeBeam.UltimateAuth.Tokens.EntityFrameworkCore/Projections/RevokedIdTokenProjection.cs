using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

internal sealed class RevokedTokenIdProjection
{
    public long Id { get; set; }
    public TenantKey Tenant { get; set; }

    public string Jti { get; set; } = default!;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset RevokedAt { get; set; }

    public long Version { get; set; }
}
