namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserProfileInput
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}
