using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed record UserIdentifier
{
    public UserKey UserKey { get; init; }

    public UserIdentifierType Type { get; init; } // Email, Phone, Username
    public string Value { get; init; } = default!;

    public bool IsPrimary { get; init; }
    public bool IsVerified { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
