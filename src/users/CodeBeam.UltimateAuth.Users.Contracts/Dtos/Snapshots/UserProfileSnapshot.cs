namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserProfileSnapshot
{
    public string? DisplayName { get; init; }
    public string? Language { get; init; }
    public string? TimeZone { get; init; }
}
