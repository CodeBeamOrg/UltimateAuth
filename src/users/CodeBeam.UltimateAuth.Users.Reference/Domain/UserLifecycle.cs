using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed class UserLifecycle : IVersionedEntity, ISoftDeletable<UserLifecycle>, IEntitySnapshot<UserLifecycle>
{
    private UserLifecycle() { }

    public Guid Id { get; private set; }
    public TenantKey Tenant { get; private set; } = default!;
    public UserKey UserKey { get; private set; } = default!;

    public UserStatus Status { get; private set; }

    public long SecurityVersion { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public long Version { get; set; }

    public bool IsDeleted => DeletedAt != null;
    public bool IsActive => !IsDeleted && Status == UserStatus.Active;

    public UserLifecycle Snapshot()
    {
        return new UserLifecycle
        {
            Id = Id,
            Tenant = Tenant,
            UserKey = UserKey,
            Status = Status,
            SecurityVersion = SecurityVersion,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            DeletedAt = DeletedAt,
            Version = Version
        };
    }

    public static UserLifecycle Create(TenantKey tenant, UserKey userKey, DateTimeOffset now, Guid? id = null)
    {
        return new UserLifecycle
        {
            Id = id ?? Guid.NewGuid(),
            Tenant = tenant,
            UserKey = userKey,
            Status = UserStatus.Active,
            CreatedAt = now,
            SecurityVersion = 0,
            Version = 0
        };
    }

    public UserLifecycle MarkDeleted(DateTimeOffset now)
    {
        if (IsDeleted)
            return this;

        DeletedAt = now;
        UpdatedAt = now;
        SecurityVersion++;

        return this;
    }

    public UserLifecycle Activate(DateTimeOffset now)
    {
        if (Status == UserStatus.Active)
            return this;

        Status = UserStatus.Active;
        UpdatedAt = now;
        return this;
    }

    public UserLifecycle ChangeStatus(DateTimeOffset now, UserStatus newStatus)
    {
        if (Status == newStatus)
            return this;

        Status = newStatus;
        UpdatedAt = now;
        return this;
    }

    public UserLifecycle IncrementSecurityVersion()
    {
        SecurityVersion++;
        return this;
    }
}
