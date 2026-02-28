using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UAuthSession : IVersionedEntity
{
    public AuthSessionId SessionId { get; }
    public TenantKey Tenant { get; }
    public UserKey UserKey { get; }
    public SessionChainId ChainId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public bool IsRevoked { get; }
    public DateTimeOffset? RevokedAt { get; }
    public long SecurityVersionAtCreation { get; }
    public ClaimsSnapshot Claims { get; }
    public SessionMetadata Metadata { get; }
    public long Version { get; }

    private UAuthSession(
        AuthSessionId sessionId,
        TenantKey tenant,
        UserKey userKey,
        SessionChainId chainId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersionAtCreation,
        ClaimsSnapshot claims,
        SessionMetadata metadata,
        long version)
    {
        SessionId = sessionId;
        Tenant = tenant;
        UserKey = userKey;
        ChainId = chainId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
        SecurityVersionAtCreation = securityVersionAtCreation;
        Claims = claims;
        Metadata = metadata;
        Version = version;
    }

    public static UAuthSession Create(
        AuthSessionId sessionId,
        TenantKey tenant,
        UserKey userKey,
        SessionChainId chainId,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        long securityVersion,
        ClaimsSnapshot? claims,
        SessionMetadata metadata)
    {
        return new(
            sessionId,
            tenant,
            userKey,
            chainId,
            createdAt: now,
            expiresAt: expiresAt,
            isRevoked: false,
            revokedAt: null,
            securityVersionAtCreation: securityVersion,
            claims: claims ?? ClaimsSnapshot.Empty,
            metadata: metadata,
            version: 0
        );
    }

    public UAuthSession Revoke(DateTimeOffset at)
    {
        if (IsRevoked) return this;

        return new UAuthSession(
            SessionId,
            Tenant,
            UserKey,
            ChainId,
            CreatedAt,
            ExpiresAt,
            true,
            at,
            SecurityVersionAtCreation,
            Claims,
            Metadata,
            Version + 1
        );
    }

    internal static UAuthSession FromProjection(
        AuthSessionId sessionId,
        TenantKey tenant,
        UserKey userKey,
        SessionChainId chainId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersionAtCreation,
        ClaimsSnapshot claims,
        SessionMetadata metadata,
        long version)
    {
        return new UAuthSession(
            sessionId,
            tenant,
            userKey,
            chainId,
            createdAt,
            expiresAt,
            isRevoked,
            revokedAt,
            securityVersionAtCreation,
            claims,
            metadata,
            version
        );
    }

    public SessionState GetState(DateTimeOffset at)
    {
        if (IsRevoked) 
            return SessionState.Revoked;

        if (at >= ExpiresAt)
            return SessionState.Expired;

        return SessionState.Active;
    }

    public UAuthSession WithChain(SessionChainId chainId)
    {
        if (!ChainId.IsUnassigned)
            throw new InvalidOperationException("Chain already assigned.");

        return new UAuthSession(
            sessionId: SessionId,
            tenant: Tenant,
            userKey: UserKey,
            chainId: chainId,
            createdAt: CreatedAt,
            expiresAt: ExpiresAt,
            isRevoked: IsRevoked,
            revokedAt: RevokedAt,
            securityVersionAtCreation: SecurityVersionAtCreation,
            claims: Claims,
            metadata: Metadata,
            version: Version + 1
        );
    }
}
