namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record CheckUserIdentifierAvailabilityRequest
{
    public required UserIdentifierType Type { get; init; }
    public required string Value { get; init; }
}
