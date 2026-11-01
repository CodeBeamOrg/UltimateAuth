namespace CodeBeam.UltimateAuth.Credentials.Policies;

public sealed record PasswordPolicyContext
{
    public required string Password { get; init; }

    public string? UserName { get; init; }

    public IReadOnlyList<string> UserIdentifiers { get; init; } = Array.Empty<string>();
}
