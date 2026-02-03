namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record AddUserIdentifierRequest
{
    public UserIdentifierType Type { get; init; }
    public string Value { get; init; } = default!;
    public bool IsPrimary { get; init; }
}
