namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UAuthSessionRoot
{
    public SessionRootId RootId { get; }
    public UserKey UserKey { get; }
    public string? TenantId { get; }
    public bool IsRevoked { get; }
    public DateTimeOffset? RevokedAt { get; }
    public long SecurityVersion { get; }
    public IReadOnlyList<UAuthSessionChain> Chains { get; }
    public DateTimeOffset LastUpdatedAt { get; }

    private UAuthSessionRoot(
        SessionRootId rootId,
        string? tenantId,
        UserKey userKey,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersion,
        IReadOnlyList<UAuthSessionChain> chains,
        DateTimeOffset lastUpdatedAt)
    {
        RootId = rootId;
        TenantId = tenantId;
        UserKey = userKey;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
        SecurityVersion = securityVersion;
        Chains = chains;
        LastUpdatedAt = lastUpdatedAt;
    }

    public static UAuthSessionRoot Create(
        string? tenantId,
        UserKey userKey,
        DateTimeOffset issuedAt)
    {
        return new UAuthSessionRoot(
            SessionRootId.New(),
            tenantId,
            userKey,
            isRevoked: false,
            revokedAt: null,
            securityVersion: 0,
            chains: Array.Empty<UAuthSessionChain>(),
            lastUpdatedAt: issuedAt
        );
    }

    public UAuthSessionRoot Revoke(DateTimeOffset at)
    {
        if (IsRevoked)
            return this;

        return new UAuthSessionRoot(
            RootId,
            TenantId,
            UserKey,
            isRevoked: true,
            revokedAt: at,
            securityVersion: SecurityVersion,
            chains: Chains,
            lastUpdatedAt: at
        );
    }

    public UAuthSessionRoot AttachChain(UAuthSessionChain chain, DateTimeOffset at)
    {
        if (IsRevoked)
            return this;

        return new UAuthSessionRoot(
            RootId,
            TenantId,
            UserKey,
            IsRevoked,
            RevokedAt,
            SecurityVersion,
            Chains.Concat(new[] { chain }).ToArray(),
            at
        );
    }

    internal static UAuthSessionRoot FromProjection(
        SessionRootId rootId,
        string? tenantId,
        UserKey userKey,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersion,
        IReadOnlyList<UAuthSessionChain> chains,
        DateTimeOffset lastUpdatedAt)
    {
        return new UAuthSessionRoot(
            rootId,
            tenantId,
            userKey,
            isRevoked,
            revokedAt,
            securityVersion,
            chains,
            lastUpdatedAt
        );
    }


}
