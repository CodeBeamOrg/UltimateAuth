namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserViewDto
{
    public string UserKey { get; init; } = default!;

    public string? UserName { get; init; }
    public string? PrimaryEmail { get; init; }
    public string? PrimaryPhone { get; init; }

    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? DisplayName { get; init; }
    public string? Bio { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? Gender { get; init; }

    public bool EmailVerified { get; init; }
    public bool PhoneVerified { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
    //public DateTimeOffset? LastLoginAt { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
