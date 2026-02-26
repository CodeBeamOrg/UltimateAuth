namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record VerifyUserIdentifierRequest
{
    public Guid IdentifierId { get; init; }
}
