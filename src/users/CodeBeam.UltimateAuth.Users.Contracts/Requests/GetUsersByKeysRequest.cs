using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record GetUsersByKeysRequest
{
    public IReadOnlyList<UserKey> UserKeys { get; init; } = Array.Empty<UserKey>();
}
