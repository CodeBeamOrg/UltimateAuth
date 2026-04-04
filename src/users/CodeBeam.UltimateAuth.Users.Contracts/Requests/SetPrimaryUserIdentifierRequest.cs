namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record SetPrimaryUserIdentifierRequest
{
    public Guid Id { get; init; }
}
