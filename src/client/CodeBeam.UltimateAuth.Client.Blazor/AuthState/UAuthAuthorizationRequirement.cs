using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Defines the authorization requirements evaluated by UltimateAuth
/// Blazor authorization components.
/// </summary>
public sealed record UAuthAuthorizationRequirement
{
    /// <summary>
    /// Comma-separated roles.
    /// </summary>
    public string? Roles { get; init; }

    /// <summary>
    /// Comma-separated permissions.
    /// </summary>
    public string? Permissions { get; init; }

    /// <summary>
    /// Optional ASP.NET Core authorization policy.
    /// </summary>
    public string? Policy { get; init; }

    /// <summary>
    /// Determines how authorization conditions are combined.
    /// </summary>
    public AuthorizationMatchMode MatchMode { get; init; } = AuthorizationMatchMode.Category;

    /// <summary>
    /// Requires an active session when enabled.
    /// </summary>
    public bool RequireActive { get; init; } = true;
}
