using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed record UserProfileQuery : PageRequest
{
    public bool IncludeDeleted { get; init; }
    public ProfileKey? ProfileKey { get; set; }
}
