using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserLifecycleQuery : PageRequest
{
    public bool IncludeDeleted { get; init; }
    public UserStatus? Status { get; init; }
}
