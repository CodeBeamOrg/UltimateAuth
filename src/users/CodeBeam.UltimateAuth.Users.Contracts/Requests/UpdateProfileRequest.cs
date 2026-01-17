namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UpdateProfileRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? DisplayName { get; init; }
    public string? PhoneNumber { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
