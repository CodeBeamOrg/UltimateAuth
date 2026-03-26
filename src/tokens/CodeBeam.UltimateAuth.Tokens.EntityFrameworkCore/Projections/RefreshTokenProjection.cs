using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

public sealed class RefreshTokenProjection
{
    public long Id { get; set; } // EF PK

    public TokenId TokenId { get; set; }

    public TenantKey Tenant { get; set; }

    public string TokenHash { get; set; } = default!;

    public UserKey UserKey { get; set; } = default!;

    public AuthSessionId SessionId { get; set; } = default!;

    public SessionChainId? ChainId { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public long Version { get; set; }
}
