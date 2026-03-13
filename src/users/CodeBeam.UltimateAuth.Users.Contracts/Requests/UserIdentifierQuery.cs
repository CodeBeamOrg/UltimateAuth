using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class UserIdentifierQuery : PageRequest
{
    public UserKey? UserKey { get; set; }

    public bool IncludeDeleted { get; init; } = false;
}
