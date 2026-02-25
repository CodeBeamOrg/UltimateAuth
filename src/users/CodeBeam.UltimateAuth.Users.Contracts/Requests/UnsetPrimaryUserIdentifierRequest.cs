namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UnsetPrimaryUserIdentifierRequest
{
    public Guid IdentifierId { get; init; }
}
