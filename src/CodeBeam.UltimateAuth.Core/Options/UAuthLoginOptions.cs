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
    /// Duration for which the user is locked out after exceeding <see cref="MaxFailedAttempts" />.
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets a value indicating whether detailed information about authentication failures (like locked until or
    /// remaining attempts) is included in responses.
    /// </summary>
    public bool IncludeFailureDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets the time interval during which failed login attempts are counted for lockout purposes.
    /// </summary>
    /// <remarks>This property defines the window of time used to evaluate consecutive failed login attempts.
    /// If the number of failures within this window exceeds the configured threshold, the account may be locked out.
    /// Adjusting this value affects how quickly lockout conditions are triggered.
    /// </remarks>
    public TimeSpan FailureWindow { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets a value indicating whether the lock should be extended when a login attempt fails during lockout.
    /// </summary>
    /// <remarks>Set this property to <see langword="true"/> to automatically extend the lock duration after
    /// each failed login attempt. This can help prevent repeated unauthorized access attempts by increasing the lockout period.
    /// </remarks>
    public bool ExtendLockOnFailure { get; set; } = false;

    internal UAuthLoginOptions Clone() => new()
    {
        MaxFailedAttempts = MaxFailedAttempts,
        LockoutDuration = LockoutDuration,
        IncludeFailureDetails = IncludeFailureDetails,
        FailureWindow = FailureWindow,
        ExtendLockOnFailure = ExtendLockOnFailure
    };
}
