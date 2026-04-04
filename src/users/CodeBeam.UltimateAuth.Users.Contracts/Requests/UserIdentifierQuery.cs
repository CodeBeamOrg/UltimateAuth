using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserIdentifierQuery : PageRequest
{
    public UserKey? UserKey { get; init; }

    public bool IncludeDeleted { get; init; }
}
