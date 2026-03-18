using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

// TODO: Multi profile (e.g., public profiles, private profiles, profiles per application, etc. with ProfileKey)
public sealed class UserProfile : ITenantEntity, IVersionedEntity, ISoftDeletable<UserProfile>, IEntitySnapshot<UserProfile>
{
    private UserProfile() { }

    public Guid Id { get; private set; }
    public TenantKey Tenant { get; private set; }

    public UserKey UserKey { get; init; } = default!;

    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? DisplayName { get; private set; }

    public DateOnly? BirthDate { get; private set; }
    public string? Gender { get; private set; }
    public string? Bio { get; private set; }

    public string? Language { get; private set; }
    public string? TimeZone { get; private set; }
    public string? Culture { get; private set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; private set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public long Version { get; set; }

    public bool IsDeleted => DeletedAt != null;
    public string EffectiveDisplayName => DisplayName ?? $"{FirstName} {LastName}";

    public UserProfile Snapshot()
    {
        return new UserProfile
        {
            Id = Id,
            Tenant = Tenant,
            UserKey = UserKey,
            FirstName = FirstName,
            LastName = LastName,
            DisplayName = DisplayName,
            BirthDate = BirthDate,
            Gender = Gender,
            Bio = Bio,
            Language = Language,
            TimeZone = TimeZone,
            Culture = Culture,
            Metadata = Metadata,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            DeletedAt = DeletedAt,
            Version = Version
        };
    }

    public static UserProfile Create(
        DateTimeOffset createdAt,
        TenantKey tenant,
        UserKey userKey,
        Guid? id = null,
        string? firstName = null,
        string? lastName = null,
        string? displayName = null,
        DateOnly? birthDate = null,
        string? gender = null,
        string? bio = null,
        string? language = null,
        string? timezone = null,
        string? culture = null)
    {
        return new UserProfile
        {
            Id = id ?? Guid.NewGuid(),
            Tenant = tenant,
            UserKey = userKey,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            BirthDate = birthDate,
            Gender = gender,
            Bio = bio,
            Language = language,
            TimeZone = timezone,
            Culture = culture,
            CreatedAt = createdAt,
            UpdatedAt = null,
            DeletedAt = null,
            Metadata = null,
            Version = 0
        };
    }

    public UserProfile UpdateName(string? firstName, string? lastName, string? displayName, DateTimeOffset now)
    {
        if (FirstName == firstName &&
            LastName == lastName &&
            DisplayName == displayName)
            return this;

        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        UpdatedAt = now;

        return this;
    }

    public UserProfile UpdatePersonalInfo(DateOnly? birthDate, string? gender, string? bio, DateTimeOffset now)
    {
        if (BirthDate == birthDate &&
            Gender == gender &&
            Bio == bio)
            return this;

        BirthDate = birthDate;
        Gender = gender;
        Bio = bio;
        UpdatedAt = now;

        return this;
    }

    public UserProfile UpdateLocalization(string? language, string? timeZone, string? culture, DateTimeOffset now)
    {
        if (Language == language &&
            TimeZone == timeZone &&
            Culture == culture)
            return this;

        Language = language;
        TimeZone = timeZone;
        Culture = culture;
        UpdatedAt = now;

        return this;
    }

    public UserProfile UpdateMetadata(IReadOnlyDictionary<string, string>? metadata, DateTimeOffset now)
    {
        if (Metadata == metadata)
            return this;

        Metadata = metadata;
        UpdatedAt = now;

        return this;
    }

    public UserProfile MarkDeleted(DateTimeOffset now)
    {
        if (IsDeleted)
            return this;

        DeletedAt = now;
        UpdatedAt = now;

        return this;
    }

    public static UserProfile FromProjection(
        Guid id,
        TenantKey tenant,
        UserKey userKey,
        string? firstName,
        string? lastName,
        string? displayName,
        DateOnly? birthDate,
        string? gender,
        string? bio,
        string? language,
        string? timeZone,
        string? culture,
        IReadOnlyDictionary<string, string>? metadata,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        DateTimeOffset? deletedAt,
        long version)
    {
        return new UserProfile
        {
            Id = id,
            Tenant = tenant,
            UserKey = userKey,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            BirthDate = birthDate,
            Gender = gender,
            Bio = bio,
            Language = language,
            TimeZone = timeZone,
            Culture = culture,
            Metadata = metadata,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt,
            Version = version
        };
    }
}
