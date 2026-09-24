namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Specifies how the UltimateAuth client authentication state handles a state event.
/// </summary>
public enum UAuthStateEventHandlingMode
{
    /// <summary>
    /// Applies the event as a local update to the current authentication state
    /// without performing full state validation.
    /// </summary>
    Patch,

    /// <summary>
    /// Revalidates the authentication state in response to the event.
    /// </summary>
    Validate,

    /// <summary>
    /// Performs no authentication state update in response to the event.
    /// </summary>
    None
}
