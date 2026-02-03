namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UnsetPrimaryUserIdentifierRequest
{
    public UserIdentifierType Type { get; init; }
    public string Value { get; init; } = default!;
}
