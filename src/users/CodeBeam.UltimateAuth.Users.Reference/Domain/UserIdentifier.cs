using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed class UserIdentifier : ITenantEntity, IVersionedEntity, ISoftDeletable<UserIdentifier>, IEntitySnapshot<UserIdentifier>
{
    public Guid Id { get; private set; }
    public TenantKey Tenant { get; private set; }
    public UserKey UserKey { get; init; }

    public UserIdentifierType Type { get; init; } // Email, Phone, Username
    public string Value { get; private set; } = default!;
    public string NormalizedValue { get; private set; } = default!;

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public long Version { get; set; }


    public bool IsDeleted => DeletedAt is not null;
    public bool IsVerified => VerifiedAt is not null;

    public UserIdentifier Snapshot()
    {
        return new UserIdentifier
        {
            Id = Id,
            Tenant = Tenant,
            UserKey = UserKey,
            Type = Type,
            Value = Value,
            NormalizedValue = NormalizedValue,
            IsPrimary = IsPrimary,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            VerifiedAt = VerifiedAt,
            DeletedAt = DeletedAt,
            Version = Version
        };
    }

    public static UserIdentifier Create(
        Guid? id,
        TenantKey tenant,
        UserKey userKey,
        UserIdentifierType type,
        string value,
        string normalizedValue,
        DateTimeOffset now,
        bool isPrimary = false,
        DateTimeOffset? verifiedAt = null)
    {
        return new UserIdentifier
        {
            Id = id ?? Guid.NewGuid(),
            Tenant = tenant,
            UserKey = userKey,
            Type = type,
            Value = value,
            NormalizedValue = normalizedValue,
            IsPrimary = isPrimary,
            VerifiedAt = verifiedAt,
            CreatedAt = now,
            Version = 0
        };
    }

    public UserIdentifier ChangeValue(string newRawValue, string newNormalizedValue, DateTimeOffset now)
    {
        if (IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (NormalizedValue == newNormalizedValue)
            throw new UAuthIdentifierConflictException("identifier_value_unchanged");

        Value = newRawValue;
        NormalizedValue = newNormalizedValue;

        VerifiedAt = null;
        UpdatedAt = now;

        return this;
    }

    public UserIdentifier MarkVerified(DateTimeOffset at)
    {
        if (IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (IsVerified)
            throw new UAuthIdentifierConflictException("identifier_already_verified");

        VerifiedAt = at;
        UpdatedAt = at;

        return this;
    }

    public UserIdentifier SetPrimary(DateTimeOffset at)
    {
        if (IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (IsPrimary)
            return this;

        IsPrimary = true;
        UpdatedAt = at;

        return this;
    }

    public UserIdentifier UnsetPrimary(DateTimeOffset at)
    {
        if (IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (!IsPrimary)
            throw new UAuthIdentifierConflictException("identifier_is_not_primary");

        IsPrimary = false;
        UpdatedAt = at;

        return this;
    }

    public UserIdentifier MarkDeleted(DateTimeOffset at)
    {
        if (IsDeleted)
            throw new UAuthIdentifierConflictException("identifier_already_deleted");

        DeletedAt = at;
        IsPrimary = false;
        UpdatedAt = at;

        return this;
    }

    public static UserIdentifier FromProjection(
        Guid id,
        TenantKey tenant,
        UserKey userKey,
        UserIdentifierType type,
        string value,
        string normalizedValue,
        bool isPrimary,
        DateTimeOffset createdAt,
        DateTimeOffset? verifiedAt,
        DateTimeOffset? updatedAt,
        DateTimeOffset? deletedAt,
        long version)
    {
        return new UserIdentifier
        {
            Id = id,
            Tenant = tenant,
            UserKey = userKey,
            Type = type,
            Value = value,
            NormalizedValue = normalizedValue,
            IsPrimary = isPrimary,
            CreatedAt = createdAt,
            VerifiedAt = verifiedAt,
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt,
            Version = version
        };
    }

    public UserIdentifierInfo ToDto()
    {
        return new UserIdentifierInfo()
        {
            Id = Id,
            Type = Type,
            Value = Value,
            NormalizedValue = NormalizedValue,
            CreatedAt = CreatedAt,
            IsPrimary = IsPrimary,
            IsVerified = IsVerified,
            VerifiedAt = VerifiedAt,
            Version = Version
        };
    }
}
