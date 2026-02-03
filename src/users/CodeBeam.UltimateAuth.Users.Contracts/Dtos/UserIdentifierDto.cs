namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserIdentifierDto
{
    public required UserIdentifierType Type { get; init; }
    public required string Value { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsVerified { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? VerifiedAt { get; init; }
}
