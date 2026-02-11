using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Options;

/// <summary>
/// Configuration settings related to interactive user login behavior, including lockout policies and failed-attempt thresholds.
/// </summary>
public sealed class UAuthLoginOptions
{
    /// <summary>
    /// Maximum number of consecutive failed login attempts allowed before the user is locked out.
    /// Set to 0 to disable lockout entirely.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 10;

    /// <summary>
    /// Duration (in minutes) for which the user is locked out after exceeding <see cref="MaxFailedAttempts" />.
    /// </summary>
    public int LockoutMinutes { get; set; } = 15;

    internal UAuthLoginOptions Clone() => new()
    {
        MaxFailedAttempts = MaxFailedAttempts,
        LockoutMinutes = LockoutMinutes
    };
}
