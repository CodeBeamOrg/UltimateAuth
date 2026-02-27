using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed class UserLifecycleQuery : PageRequest
{
    public bool IncludeDeleted { get; init; }
    public UserStatus? Status { get; init; }
}
