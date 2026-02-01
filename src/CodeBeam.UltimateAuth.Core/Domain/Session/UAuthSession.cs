namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UAuthSession
{
    public AuthSessionId SessionId { get; }
    public string? TenantId { get; }
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

    private UAuthSession(
    AuthSessionId sessionId,
    string? tenantId,
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
    SessionMetadata metadata)
    {
        SessionId = sessionId;
        TenantId = tenantId;
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
    }

    public static UAuthSession Create(
        AuthSessionId sessionId,
        string? tenantId,
        UserKey userKey,
        SessionChainId chainId,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        DeviceContext device,
        ClaimsSnapshot claims,
        SessionMetadata metadata)
    {
        return new(
            sessionId,
            tenantId,
            userKey,
            chainId,
            createdAt: now,
            expiresAt: expiresAt,
            lastSeenAt: now,
            isRevoked: false,
            revokedAt: null,
            securityVersionAtCreation: 0,
            device: device,
            claims: claims,
            metadata: metadata
        );
    }

    public UAuthSession WithSecurityVersion(long version)
    {
        if (SecurityVersionAtCreation == version)
            return this;

        return new UAuthSession(
            SessionId,
            TenantId,
            UserKey,
            ChainId,
            CreatedAt,
            ExpiresAt,
            LastSeenAt,
            IsRevoked,
            RevokedAt,
            version,
            Device,
            Claims,
            Metadata
        );
    }

    public UAuthSession Touch(DateTimeOffset at)
    {
        return new UAuthSession(
            SessionId,
            TenantId,
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
            Metadata
        );
    }

    public UAuthSession Revoke(DateTimeOffset at)
    {
        if (IsRevoked) return this;

        return new UAuthSession(
            SessionId,
            TenantId,
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
            Metadata
        );
    }

    internal static UAuthSession FromProjection(
        AuthSessionId sessionId,
        string? tenantId,
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
        SessionMetadata metadata)
    {
        return new UAuthSession(
            sessionId,
            tenantId,
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
            metadata
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
            tenantId: TenantId,
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
            metadata: Metadata
        );
    }

}
