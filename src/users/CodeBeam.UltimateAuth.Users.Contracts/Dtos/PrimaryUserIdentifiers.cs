
namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record PrimaryUserIdentifiers
{
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? DisplayName { get; init; }
}
