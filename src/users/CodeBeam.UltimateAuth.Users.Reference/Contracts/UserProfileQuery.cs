using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed record UserProfileQuery : PageRequest
{
    public bool IncludeDeleted { get; init; }
}
