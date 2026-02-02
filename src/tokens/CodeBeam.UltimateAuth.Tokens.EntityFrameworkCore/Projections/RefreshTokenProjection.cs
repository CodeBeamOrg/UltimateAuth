using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

// Add mapper class if needed (adding domain rules etc.)
internal sealed class RefreshTokenProjection
{
    public long Id { get; set; }               // Surrogate PK
    public TenantKey Tenant { get; set; }

    public string TokenHash { get; set; } = default!;
    public UserKey UserKey { get; set; } = default!;
    public AuthSessionId SessionId { get; set; } = default!;
    public SessionChainId ChainId { get; set; } = default!;

    public string? ReplacedByTokenHash { get; set; }

    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public byte[] RowVersion { get; set; } = default!;
}
