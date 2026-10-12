using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserProfileQuery : PageRequest
{
    public bool IncludeDeleted { get; init; }
    public ProfileKey? ProfileKey { get; set; }
}
