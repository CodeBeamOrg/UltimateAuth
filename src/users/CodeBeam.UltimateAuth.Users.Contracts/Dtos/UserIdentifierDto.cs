using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserIdentifierDto : IVersionedEntity
{
    public Guid Id { get; set; }
    public required UserIdentifierType Type { get; set; }
    public required string Value { get; set; }
    public string NormalizedValue { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public long Version { get; set; }
}
