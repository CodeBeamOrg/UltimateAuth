namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record SetPrimaryUserIdentifierRequest
{
    public Guid IdentifierId { get; init; }
}
