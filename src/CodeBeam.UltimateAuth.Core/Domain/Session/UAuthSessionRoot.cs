using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UAuthSessionRoot : IVersionedEntity
{
    public SessionRootId RootId { get; }
    public UserKey UserKey { get; }
    public TenantKey Tenant { get; }
    public bool IsRevoked { get; }
    public DateTimeOffset? RevokedAt { get; }
    public long SecurityVersion { get; }
    public IReadOnlyList<UAuthSessionChain> Chains { get; }
    public DateTimeOffset LastUpdatedAt { get; }
    public long Version { get; }

    private UAuthSessionRoot(
        SessionRootId rootId,
        TenantKey tenant,
        UserKey userKey,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersion,
        IReadOnlyList<UAuthSessionChain> chains,
        DateTimeOffset lastUpdatedAt,
        long version)
    {
        RootId = rootId;
        Tenant = tenant;
        UserKey = userKey;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
        SecurityVersion = securityVersion;
        Chains = chains;
        LastUpdatedAt = lastUpdatedAt;
        Version = version;
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
            false,
            null,
            0,
            chains: Array.Empty<UAuthSessionChain>(),
            issuedAt,
            0
        );
    }

    public UAuthSessionRoot IncreaseSecurityVersion(DateTimeOffset at)
    {
        return new UAuthSessionRoot(
            RootId,
            Tenant,
            UserKey,
            IsRevoked,
            RevokedAt,
            SecurityVersion + 1,
            Chains,
            at,
            Version + 1
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
            true,
            at,
            SecurityVersion + 1,
            Chains,
            at,
            Version + 1
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
            at,
            Version + 1
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
        DateTimeOffset lastUpdatedAt,
        long version)
    {
        return new UAuthSessionRoot(
            rootId,
            tenant,
            userKey,
            isRevoked,
            revokedAt,
            securityVersion,
            chains,
            lastUpdatedAt,
            version
        );
    }
}
