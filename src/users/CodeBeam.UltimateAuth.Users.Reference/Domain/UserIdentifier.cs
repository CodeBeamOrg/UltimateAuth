using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;
public sealed record UserIdentifier : IVersionedEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    public TenantKey Tenant { get; set; }

    public UserKey UserKey { get; init; }

    public UserIdentifierType Type { get; init; } // Email, Phone, Username
    public string Value { get; set; } = default!;
    public string NormalizedValue { get; set; } = default!;

    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public long Version { get; set; }


    public bool IsDeleted => DeletedAt is not null;

    public UserIdentifier Cloned()
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
            IsVerified = IsVerified,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            VerifiedAt = VerifiedAt,
            DeletedAt = DeletedAt,
            Version = Version
        };
    }

    public void ChangeValue(string newRawValue, string newNormalizedValue, DateTimeOffset now)
    {
        if (IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (NormalizedValue == newNormalizedValue)
            throw new UAuthIdentifierConflictException("identifier_value_unchanged");

        Value = newRawValue;
        NormalizedValue = newNormalizedValue;

        IsVerified = false;
        VerifiedAt = null;
        UpdatedAt = now;

        Version++;
    }

    public void MarkVerified(DateTimeOffset at)
    {
        if (IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (IsVerified)
            throw new UAuthIdentifierConflictException("identifier_already_verified");

        IsVerified = true;
        VerifiedAt = at;
        UpdatedAt = at;

        Version++;
    }

    public void SetPrimary(DateTimeOffset at)
    {
        if (IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (IsPrimary)
            return;

        IsPrimary = true;
        UpdatedAt = at;

        Version++;
    }

    public void UnsetPrimary(DateTimeOffset at)
    {
        if (IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (!IsPrimary)
            throw new UAuthIdentifierConflictException("identifier_is_not_primary_already");

        IsPrimary = false;
        UpdatedAt = at;

        Version++;
    }

    public void MarkDeleted(DateTimeOffset at)
    {
        if (IsDeleted)
            throw new UAuthIdentifierConflictException("identifier_already_deleted");

        DeletedAt = at;
        IsPrimary = false;
        UpdatedAt = at;

        Version++;
    }
}
