namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UnsetPrimaryUserIdentifierRequest
{
    public Guid Id { get; init; }
}
