using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UAuthSessionRoot
{
    public SessionRootId RootId { get; }
    public UserKey UserKey { get; }
    public TenantKey Tenant { get; }
    public bool IsRevoked { get; }
    public DateTimeOffset? RevokedAt { get; }
    public long SecurityVersion { get; }
    public IReadOnlyList<UAuthSessionChain> Chains { get; }
    public DateTimeOffset LastUpdatedAt { get; }

    private UAuthSessionRoot(
        SessionRootId rootId,
        TenantKey tenant,
        UserKey userKey,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersion,
        IReadOnlyList<UAuthSessionChain> chains,
        DateTimeOffset lastUpdatedAt)
    {
        RootId = rootId;
        Tenant = tenant;
        UserKey = userKey;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
        SecurityVersion = securityVersion;
        Chains = chains;
        LastUpdatedAt = lastUpdatedAt;
    }

    public static UAuthSessionRoot Create(
        TenantKey tenant,
        UserKey userKey,
        DateTimeOffset issuedAt)
    {
        return new UAuthSessionRoot(
            SessionRootId.New(),
            tenant,
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
            Tenant,
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
            Tenant,
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
        TenantKey tenant,
        UserKey userKey,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersion,
        IReadOnlyList<UAuthSessionChain> chains,
        DateTimeOffset lastUpdatedAt)
    {
        return new UAuthSessionRoot(
            rootId,
            tenant,
            userKey,
            isRevoked,
            revokedAt,
            securityVersion,
            chains,
            lastUpdatedAt
        );
    }


}
