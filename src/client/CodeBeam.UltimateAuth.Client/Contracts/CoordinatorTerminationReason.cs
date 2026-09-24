namespace CodeBeam.UltimateAuth.Client.Contracts;

/// <summary>
/// Specifies why an UltimateAuth session coordinator stopped its active coordination cycle.
/// </summary>
public enum CoordinatorTerminationReason
{
    /// <summary>
    /// Indicates that no specific termination reason was reported.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the current authentication context requires the user to reauthenticate.
    /// </summary>
    ReauthRequired = 10
}
