namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UAuthSessionChain
{
    public SessionChainId ChainId { get; }
    public SessionRootId RootId { get; }
    public string? TenantId { get; }
    public UserKey UserKey { get; }
    public int RotationCount { get; }
    public long SecurityVersionAtCreation { get; }
    public ClaimsSnapshot ClaimsSnapshot { get; }
    public AuthSessionId? ActiveSessionId { get; }
    public bool IsRevoked { get; }
    public DateTimeOffset? RevokedAt { get; }

    private UAuthSessionChain(
        SessionChainId chainId,
        SessionRootId rootId,
        string? tenantId,
        UserKey userKey,
        int rotationCount,
        long securityVersionAtCreation,
        ClaimsSnapshot claimsSnapshot,
        AuthSessionId? activeSessionId,
        bool isRevoked,
        DateTimeOffset? revokedAt)
    {
        ChainId = chainId;
        RootId = rootId;
        TenantId = tenantId;
        UserKey = userKey;
        RotationCount = rotationCount;
        SecurityVersionAtCreation = securityVersionAtCreation;
        ClaimsSnapshot = claimsSnapshot;
        ActiveSessionId = activeSessionId;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
    }

    public static UAuthSessionChain Create(
        SessionChainId chainId,
        SessionRootId rootId,
        string? tenantId,
        UserKey userKey,
        long securityVersion,
        ClaimsSnapshot claimsSnapshot)
    {
        return new UAuthSessionChain(
            chainId,
            rootId,
            tenantId,
            userKey,
            rotationCount: 0,
            securityVersionAtCreation: securityVersion,
            claimsSnapshot: claimsSnapshot,
            activeSessionId: null,
            isRevoked: false,
            revokedAt: null
        );
    }

    public UAuthSessionChain AttachSession(AuthSessionId sessionId)
    {
        if (IsRevoked)
            return this;

        return new UAuthSessionChain(
            ChainId,
            RootId,
            TenantId,
            UserKey,
            RotationCount, // Unchanged on first attach
            SecurityVersionAtCreation,
            ClaimsSnapshot,
            activeSessionId: sessionId,
            isRevoked: false,
            revokedAt: null
        );
    }

    public UAuthSessionChain RotateSession(AuthSessionId sessionId)
    {
        if (IsRevoked)
            return this;

        return new UAuthSessionChain(
            ChainId,
            RootId,
            TenantId,
            UserKey,
            RotationCount + 1,
            SecurityVersionAtCreation,
            ClaimsSnapshot,
            activeSessionId: sessionId,
            isRevoked: false,
            revokedAt: null
        );
    }

    public UAuthSessionChain Revoke(DateTimeOffset at)
    {
        if (IsRevoked)
            return this;

        return new UAuthSessionChain(
            ChainId,
            RootId,
            TenantId,
            UserKey,
            RotationCount,
            SecurityVersionAtCreation,
            ClaimsSnapshot,
            ActiveSessionId,
            isRevoked: true,
            revokedAt: at
        );
    }

    internal static UAuthSessionChain FromProjection(
        SessionChainId chainId,
        SessionRootId rootId,
        string? tenantId,
        UserKey userKey,
        int rotationCount,
        long securityVersionAtCreation,
        ClaimsSnapshot claimsSnapshot,
        AuthSessionId? activeSessionId,
        bool isRevoked,
        DateTimeOffset? revokedAt)
    {
        return new UAuthSessionChain(
            chainId,
            rootId,
            tenantId,
            userKey,
            rotationCount,
            securityVersionAtCreation,
            claimsSnapshot,
            activeSessionId,
            isRevoked,
            revokedAt
        );
    }

}
