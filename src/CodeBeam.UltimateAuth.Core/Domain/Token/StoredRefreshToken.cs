using System.ComponentModel.DataAnnotations.Schema;

namespace CodeBeam.UltimateAuth.Core.Domain;

/// <summary>
/// Represents a persisted refresh token bound to a session.
/// Stored as a hashed value for security reasons.
/// </summary>
public sealed record StoredRefreshToken
{
    public string TokenHash { get; init; } = default!;

    public string? TenantId { get; init; }

    public required UserKey UserKey { get; init; }

    public AuthSessionId SessionId { get; init; } = default!;
    public SessionChainId? ChainId { get; init; }

    public DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }

    public string? ReplacedByTokenHash { get; init; }

    [NotMapped]
    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now) && ReplacedByTokenHash is null;
}
