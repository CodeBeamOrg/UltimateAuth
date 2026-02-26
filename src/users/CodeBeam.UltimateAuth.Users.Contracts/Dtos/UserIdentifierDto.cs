namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserIdentifierDto
{
    public Guid Id { get; set; }
    public required UserIdentifierType Type { get; set; }
    public required string Value { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? VerifiedAt { get; set; }
}
