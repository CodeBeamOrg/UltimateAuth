using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserProfileInfo : IVersionedEntity
{
    public Guid Id { get; set; }

    public ProfileKey ProfileKey { get; set; } = ProfileKey.Default;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? DisplayName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Gender { get; set; }

    public string? Bio { get; set; }

    public string? Language { get; set; }

    public string? TimeZone { get; set; }

    public string? Culture { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public long Version { get; set; }
}
