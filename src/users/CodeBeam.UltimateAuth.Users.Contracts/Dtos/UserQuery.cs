using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class UserQuery : PageRequest
{
    public string? Search { get; set; }
    public UserStatus? Status { get; set; }
    public bool IncludeDeleted { get; set; }
}
