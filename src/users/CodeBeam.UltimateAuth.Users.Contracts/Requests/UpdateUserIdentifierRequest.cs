namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UpdateUserIdentifierRequest
{
    public Guid Id { get; init; }
    public required string NewValue { get; init; }
}
