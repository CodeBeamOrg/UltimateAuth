namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record IdentifierExistsRequest
{
    public UserIdentifierType Type { get; init; }
    public required string Value { get; set; }
}
