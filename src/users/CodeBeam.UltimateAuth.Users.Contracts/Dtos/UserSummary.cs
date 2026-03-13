using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserSummary
{
    public UserKey UserKey { get; init; } = default!;

    public string? UserName { get; init; }

    public string? DisplayName { get; init; }

    public string? PrimaryEmail { get; init; }

    public string? PrimaryPhone { get; init; }

    public UserStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
