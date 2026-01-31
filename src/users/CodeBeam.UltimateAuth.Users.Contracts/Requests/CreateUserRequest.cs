namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record CreateUserRequest
{
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

    public UserIdentifierType? PrimaryIdentifierType { get; init; }
    public string? PrimaryIdentifierValue { get; init; }
    public bool PrimaryIdentifierVerified { get; init; } = false;
}
