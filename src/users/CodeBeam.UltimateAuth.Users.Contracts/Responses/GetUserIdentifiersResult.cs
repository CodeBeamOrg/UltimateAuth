namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record GetUserIdentifiersResult
{
    public required IReadOnlyCollection<UserIdentifierDto> Identifiers { get; init; }
}
