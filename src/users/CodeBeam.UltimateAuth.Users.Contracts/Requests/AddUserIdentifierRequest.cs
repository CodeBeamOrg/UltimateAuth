namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record AddUserIdentifierRequest
{
    public UserIdentifierType Type { get; init; }
    public required string Value { get; init; }
    public bool IsPrimary { get; init; }
}
