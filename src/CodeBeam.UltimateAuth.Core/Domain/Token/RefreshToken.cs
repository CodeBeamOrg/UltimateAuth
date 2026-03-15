using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed record RefreshToken : IVersionedEntity
{
    public TokenId TokenId { get; init; }

    public string TokenHash { get; init; } = default!;

    public TenantKey Tenant { get; init; }

    public required UserKey UserKey { get; init; }

    public AuthSessionId SessionId { get; init; }

    public SessionChainId? ChainId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset? RevokedAt { get; init; }

    public string? ReplacedByTokenHash { get; init; }

    public long Version { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired(DateTimeOffset now)
        => ExpiresAt <= now;

    public bool IsActive(DateTimeOffset now)
        => !IsRevoked && !IsExpired(now) && ReplacedByTokenHash is null;

    public static RefreshToken Create(
        TokenId tokenId,
        string tokenHash,
        TenantKey tenant,
        UserKey userKey,
        AuthSessionId sessionId,
        SessionChainId? chainId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        return new RefreshToken
        {
            TokenId = tokenId,
            TokenHash = tokenHash,
            Tenant = tenant,
            UserKey = userKey,
            SessionId = sessionId,
            ChainId = chainId,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            Version = 0
        };
    }

    public RefreshToken Revoke(DateTimeOffset at, string? replacedBy = null)
    {
        if (IsRevoked)
            return this;

        return this with
        {
            RevokedAt = at,
            ReplacedByTokenHash = replacedBy,
            Version = Version + 1
        };
    }

    public RefreshToken Replace(string newTokenHash, DateTimeOffset at)
    {
        if (IsRevoked)
            throw new UAuthConflictException("Token already revoked.");

        return this with
        {
            RevokedAt = at,
            ReplacedByTokenHash = newTokenHash,
            Version = Version + 1
        };
    }
}
