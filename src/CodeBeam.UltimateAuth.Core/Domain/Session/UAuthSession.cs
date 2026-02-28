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
    public DateTimeOffset? LastSeenAt { get; }
    public bool IsRevoked { get; }
    public DateTimeOffset? RevokedAt { get; }
    public long SecurityVersionAtCreation { get; }
    public DeviceContext Device { get; }
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
        DateTimeOffset? lastSeenAt,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersionAtCreation,
        DeviceContext device,
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
        LastSeenAt = lastSeenAt;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
        SecurityVersionAtCreation = securityVersionAtCreation;
        Device = device;
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
        DeviceContext device,
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
            lastSeenAt: now,
            isRevoked: false,
            revokedAt: null,
            securityVersionAtCreation: 0,
            device: device,
            claims: claims ?? ClaimsSnapshot.Empty,
            metadata: metadata,
            version: 0
        );
    }

    public UAuthSession WithSecurityVersion(long securityVersion)
    {
        if (SecurityVersionAtCreation == securityVersion)
            return this;

        return new UAuthSession(
            SessionId,
            Tenant,
            UserKey,
            ChainId,
            CreatedAt,
            ExpiresAt,
            LastSeenAt,
            IsRevoked,
            RevokedAt,
            securityVersion,
            Device,
            Claims,
            Metadata,
            Version + 1
        );
    }

    public UAuthSession Touch(DateTimeOffset at)
    {
        return new UAuthSession(
            SessionId,
            Tenant,
            UserKey,
            ChainId,
            CreatedAt,
            ExpiresAt,
            at,
            IsRevoked,
            RevokedAt,
            SecurityVersionAtCreation,
            Device,
            Claims,
            Metadata,
            Version + 1
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
            LastSeenAt,
            true,
            at,
            SecurityVersionAtCreation,
            Device,
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
        DateTimeOffset? lastSeenAt,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersionAtCreation,
        DeviceContext device,
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
            lastSeenAt,
            isRevoked,
            revokedAt,
            securityVersionAtCreation,
            device,
            claims,
            metadata,
            version
        );
    }

    public SessionState GetState(DateTimeOffset at, TimeSpan? idleTimeout)
    {
        if (IsRevoked) 
            return SessionState.Revoked;

        if (at >= ExpiresAt)
            return SessionState.Expired;

        if (idleTimeout.HasValue && at - LastSeenAt >= idleTimeout.Value)
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
            lastSeenAt: LastSeenAt,
            isRevoked: IsRevoked,
            revokedAt: RevokedAt,
            securityVersionAtCreation: SecurityVersionAtCreation,
            device: Device,
            claims: Claims,
            metadata: Metadata,
            version: Version + 1
        );
    }

}
