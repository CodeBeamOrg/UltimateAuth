using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

// TODO: Multi profile (e.g., public profiles, private profiles, profiles per application, etc. with ProfileKey)
public sealed record class UserProfile : IVersionedEntity
{
    public TenantKey Tenant { get; set; }

    public UserKey UserKey { get; init; } = default!;

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

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public long Version { get; set; }
}
