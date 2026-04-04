namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record CreateUserRequest
{
    public string? UserName { get; init; }
    public bool UserNameVerified { get; init; }
    public string? Email { get; init; }
    public bool EmailVerified { get; init; }
    public string? Phone { get; init; }
    public bool PhoneVerified { get; init; }

    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? DisplayName { get; init; }

    public string? Password { get; init; }

    public DateOnly? BirthDate { get; init; }
    public string? Gender { get; init; }
    public string? Bio { get; init; }
    public string? Language { get; init; }
    public string? TimeZone { get; init; }
    public string? Culture { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
