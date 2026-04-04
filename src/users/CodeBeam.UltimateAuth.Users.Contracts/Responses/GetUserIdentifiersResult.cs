namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record GetUserIdentifiersResult
{
    public required IReadOnlyCollection<UserIdentifierInfo> Identifiers { get; init; }
}
