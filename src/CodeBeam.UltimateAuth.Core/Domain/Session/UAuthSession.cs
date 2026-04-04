using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
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
    public DateTimeOffset? RevokedAt { get; }
    public long SecurityVersionAtCreation { get; }
    public DeviceContext Device { get; } // For snapshot,main value is chain's device.
    public ClaimsSnapshot Claims { get; }
    public SessionMetadata Metadata { get; }
    public long Version { get; set; }

    public bool IsRevoked => RevokedAt != null;

    private UAuthSession(
        AuthSessionId sessionId,
        TenantKey tenant,
        UserKey userKey,
        SessionChainId chainId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
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
        long securityVersion,
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
            revokedAt: null,
            securityVersionAtCreation: securityVersion,
            device: device,
            claims: claims ?? ClaimsSnapshot.Empty,
            metadata: metadata,
            version: 0
        );
    }

    public UAuthSession Revoke(DateTimeOffset at)
    {
        if (IsRevoked)
            return this;

        return new UAuthSession(
            SessionId,
            Tenant,
            UserKey,
            ChainId,
            CreatedAt,
            ExpiresAt,
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
            revokedAt,
            securityVersionAtCreation,
            device,
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
            throw new UAuthConflictException("Chain already assigned.");

        return new UAuthSession(
            sessionId: SessionId,
            tenant: Tenant,
            userKey: UserKey,
            chainId: chainId,
            createdAt: CreatedAt,
            expiresAt: ExpiresAt,
            revokedAt: RevokedAt,
            securityVersionAtCreation: SecurityVersionAtCreation,
            device: Device,
            claims: Claims,
            metadata: Metadata,
            version: Version + 1
        );
    }
}
