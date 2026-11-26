namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Determines whether automatic authentication recovery
/// is permitted for a request.
/// </summary>
public enum UAuthRequestAuthentication
{
    /// <summary>
    /// Automatic authentication recovery is not permitted.
    /// </summary>
    None = 0,

    /// <summary>
    /// Authentication recovery is permitted, but the original request must not be automatically replayed.
    /// </summary>
    Recoverable = 10,

    /// <summary>
    /// Authentication recovery and a single request replay are permitted.
    /// </summary>
    Replayable = 20
}
