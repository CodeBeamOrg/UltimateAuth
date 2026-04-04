namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record ChangeUserIdentifierRequest
{
    public required UserIdentifierType Type { get; init; }
    public required string NewValue { get; init; }
    public string? Reason { get; init; }
}
