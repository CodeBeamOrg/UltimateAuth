using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserQuery : PageRequest
{
    /// <summary>
    /// Searches users by display name, username, primary email or primary phone using case-insensitive partial matching.
    /// UserKey is matched only when the search term represents a valid, complete UserKey; partial UserKey matching is not supported.
    /// </summary>
    public string? Search { get; set; }
    public UserStatus? Status { get; set; }
    public bool IncludeDeleted { get; set; }
    public ProfileKey? ProfileKey { get; set; }
}
