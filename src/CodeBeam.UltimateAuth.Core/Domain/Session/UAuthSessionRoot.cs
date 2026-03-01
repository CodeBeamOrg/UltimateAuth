using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UAuthSessionRoot : IVersionedEntity
{
    public SessionRootId RootId { get; }
    public TenantKey Tenant { get; }
    public UserKey UserKey { get; }

    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? UpdatedAt { get; }

    public bool IsRevoked { get; }
    public DateTimeOffset? RevokedAt { get; }

    public long SecurityVersion { get; }
    public long Version { get; }

    private UAuthSessionRoot(
        SessionRootId rootId,
        TenantKey tenant,
        UserKey userKey,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersion,
        long version)
    {
        RootId = rootId;
        Tenant = tenant;
        UserKey = userKey;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
        SecurityVersion = securityVersion;
        Version = version;
    }

    public static UAuthSessionRoot Create(
        TenantKey tenant,
        UserKey userKey,
        DateTimeOffset at)
    {
        return new UAuthSessionRoot(
            SessionRootId.New(),
            tenant,
            userKey,
            at,
            null,
            false,
            null,
            0,
            0
        );
    }

    public UAuthSessionRoot IncreaseSecurityVersion(DateTimeOffset at)
    {
        return new UAuthSessionRoot(
            RootId,
            Tenant,
            UserKey,
            CreatedAt,
            at,
            IsRevoked,
            RevokedAt,
            SecurityVersion + 1,
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
            CreatedAt,
            at,
            true,
            at,
            SecurityVersion + 1,
            Version + 1
        );
    }

    internal static UAuthSessionRoot FromProjection(
        SessionRootId rootId,
        TenantKey tenant,
        UserKey userKey,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersion,
        long version)
    {
        return new UAuthSessionRoot(
            rootId,
            tenant,
            userKey,
            createdAt,
            updatedAt,
            isRevoked,
            revokedAt,
            securityVersion,
            version
        );
    }
}
