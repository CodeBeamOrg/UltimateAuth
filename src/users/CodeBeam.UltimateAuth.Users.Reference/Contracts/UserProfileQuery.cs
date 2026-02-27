using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed class UserProfileQuery : PageRequest
{
    public bool IncludeDeleted { get; init; }
}
