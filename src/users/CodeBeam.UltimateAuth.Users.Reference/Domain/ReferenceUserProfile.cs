using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference.Domain;

public sealed record class ReferenceUserProfile
{
    public UserKey UserKey { get; init; } = default!;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; init; }
    public string? Phone { get; init; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
