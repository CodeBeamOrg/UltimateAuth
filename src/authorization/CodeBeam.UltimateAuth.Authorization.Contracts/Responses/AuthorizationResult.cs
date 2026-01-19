namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record AuthorizationResult
{
    public required bool IsAllowed { get; init; }

    /// <summary>
    /// Indicates whether re-authentication is required.
    /// </summary>
    public bool RequiresReauthentication { get; init; }

    /// <summary>
    /// Optional reason code for denial.
    /// </summary>
    public string? DenyReason { get; init; }

    public static AuthorizationResult Allow()
        => new() { IsAllowed = true };

    public static AuthorizationResult Deny(string? reason = null)
        => new() { IsAllowed = false, DenyReason = reason };

    public static AuthorizationResult ReauthRequired()
        => new() { IsAllowed = false, RequiresReauthentication = true };
}
