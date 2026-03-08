using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization;

public sealed class RoleQuery : PageRequest
{
    public string? Search { get; set; }
    public bool IncludeDeleted { get; set; }
}