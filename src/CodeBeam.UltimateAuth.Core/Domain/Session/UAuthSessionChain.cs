using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UAuthSessionChain : IVersionedEntity
{
    public SessionChainId ChainId { get; }
    public SessionRootId RootId { get; }
    public TenantKey Tenant { get; }
    public UserKey UserKey { get; }

    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastSeenAt { get; }
    public DateTimeOffset? AbsoluteExpiresAt { get; }
    public DeviceContext Device { get; }
    public ClaimsSnapshot ClaimsSnapshot { get; }
    public AuthSessionId? ActiveSessionId { get; }
    public int RotationCount { get; }
    public int TouchCount { get; }
    public long SecurityVersionAtCreation { get; }
    
    public DateTimeOffset? RevokedAt { get; }
    public long Version { get; set; }


    public bool IsActive => ActiveSessionId is not null;
    public bool IsRevoked => RevokedAt is not null;
    public SessionChainState State => IsRevoked ? SessionChainState.Revoked : ActiveSessionId is null ? SessionChainState.Passive : SessionChainState.Active;

    private UAuthSessionChain(
        SessionChainId chainId,
        SessionRootId rootId,
        TenantKey tenant,
        UserKey userKey,
        DateTimeOffset createdAt,
        DateTimeOffset lastSeenAt,
        DateTimeOffset? absoluteExpiresAt,
        DeviceContext device,
        ClaimsSnapshot claimsSnapshot,
        AuthSessionId? activeSessionId,
        int rotationCount,
        int touchCount,
        long securityVersionAtCreation,
        DateTimeOffset? revokedAt,
        long version)
    {
        ChainId = chainId;
        RootId = rootId;
        Tenant = tenant;
        UserKey = userKey;
        CreatedAt = createdAt;
        LastSeenAt = lastSeenAt;
        AbsoluteExpiresAt = absoluteExpiresAt;
        Device = device;
        ClaimsSnapshot = claimsSnapshot;
        ActiveSessionId = activeSessionId;
        RotationCount = rotationCount;
        TouchCount = touchCount;
        SecurityVersionAtCreation = securityVersionAtCreation;
        RevokedAt = revokedAt;
        Version = version;
    }

    public static UAuthSessionChain Create(
        SessionChainId chainId,
        SessionRootId rootId,
        TenantKey tenant,
        UserKey userKey,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt,
        DeviceContext device,
        ClaimsSnapshot claimsSnapshot,
        long securityVersion)
    {
        return new UAuthSessionChain(
            chainId,
            rootId,
            tenant,
            userKey,
            createdAt,
            createdAt,
            expiresAt,
            device,
            claimsSnapshot,
            null,
            rotationCount: 0,
            touchCount: 0,
            securityVersionAtCreation: securityVersion,
            revokedAt: null,
            version: 0
        );
    }

    public UAuthSessionChain AttachSession(AuthSessionId sessionId, DateTimeOffset now)
    {
        if (IsRevoked || IsExpired(now))
            return this;

        if (ActiveSessionId.HasValue && ActiveSessionId.Value.Equals(sessionId))
            return this;

        return new UAuthSessionChain(
            ChainId,
            RootId,
            Tenant,
            UserKey,
            CreatedAt,
            lastSeenAt: now,
            AbsoluteExpiresAt,
            Device,
            ClaimsSnapshot,
            activeSessionId: sessionId,
            RotationCount, // Unchanged on first attach
            TouchCount,
            SecurityVersionAtCreation,
            RevokedAt,
            Version + 1
        );
    }

    public UAuthSessionChain DetachSession(DateTimeOffset now)
    {
        if (ActiveSessionId is null)
            return this;

        return new UAuthSessionChain(
            ChainId,
            RootId,
            Tenant,
            UserKey,
            CreatedAt,
            lastSeenAt: now,
            AbsoluteExpiresAt,
            Device,
            ClaimsSnapshot,
            activeSessionId: null,
            RotationCount, // Unchanged on first attach
            TouchCount,
            SecurityVersionAtCreation,
            RevokedAt,
            Version + 1
        );
    }

    public UAuthSessionChain RotateSession(AuthSessionId sessionId, DateTimeOffset now, ClaimsSnapshot? claimsSnapshot = null)
    {
        if (IsRevoked || IsExpired(now))
            return this;

        if (ActiveSessionId.HasValue && ActiveSessionId.Value.Equals(sessionId))
            return this;

        return new UAuthSessionChain(
            ChainId,
            RootId,
            Tenant,
            UserKey,
            CreatedAt,
            lastSeenAt: now,
            AbsoluteExpiresAt,
            Device,
            claimsSnapshot ?? ClaimsSnapshot,
            activeSessionId: sessionId,
            RotationCount + 1,
            TouchCount,
            SecurityVersionAtCreation,
            RevokedAt,
            Version + 1
        );
    }

    public UAuthSessionChain Touch(DateTimeOffset now, ClaimsSnapshot? claimsSnapshot = null)
    {
        if (IsRevoked || IsExpired(now))
            return this;

        return new UAuthSessionChain(
            ChainId,
            RootId,
            Tenant,
            UserKey,
            CreatedAt,
            lastSeenAt: now,
            AbsoluteExpiresAt,
            Device,
            claimsSnapshot ?? ClaimsSnapshot,
            ActiveSessionId,
            RotationCount,
            TouchCount + 1,
            SecurityVersionAtCreation,
            RevokedAt,
            Version + 1
        );
    }

    public UAuthSessionChain Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
            return this;

        return new UAuthSessionChain(
            ChainId,
            RootId,
            Tenant,
            UserKey,
            CreatedAt,
            now,
            AbsoluteExpiresAt,
            Device,
            ClaimsSnapshot,
            ActiveSessionId,
            RotationCount,
            TouchCount,
            SecurityVersionAtCreation,
            revokedAt: now,
            Version + 1
        );
    }

    internal static UAuthSessionChain FromProjection(
        SessionChainId chainId,
        SessionRootId rootId,
        TenantKey tenant,
        UserKey userKey,
        DateTimeOffset createdAt,
        DateTimeOffset lastSeenAt,
        DateTimeOffset? expiresAt,
        DeviceContext device,
        ClaimsSnapshot claimsSnapshot,
        AuthSessionId? activeSessionId,
        int rotationCount,
        int touchCount,
        long securityVersionAtCreation,
        DateTimeOffset? revokedAt,
        long version)
    {
        return new UAuthSessionChain(
             chainId,
             rootId,
             tenant,
             userKey,
             createdAt,
             lastSeenAt,
             expiresAt,
             device,
             claimsSnapshot,
             activeSessionId,
             rotationCount,
             touchCount,
             securityVersionAtCreation,
             revokedAt,
             version
         );
    }

    private bool IsExpired(DateTimeOffset at) => AbsoluteExpiresAt.HasValue && at >= AbsoluteExpiresAt.Value;

    public SessionState GetState(DateTimeOffset at, TimeSpan? idleTimeout)
    {
        if (IsRevoked)
            return SessionState.Revoked;

        if (IsExpired(at))
            return SessionState.Expired;

        if (idleTimeout.HasValue && at - LastSeenAt >= idleTimeout.Value)
            return SessionState.Expired;

        return SessionState.Active;
    }
}
