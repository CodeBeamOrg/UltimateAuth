namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Describes why the UltimateAuth client authentication state changed.
/// </summary>
public enum UAuthStateChangeReason
{
    /// <summary>
    /// The state was updated with an authenticated identity snapshot.
    /// </summary>
    Authenticated,

    /// <summary>
    /// The current authentication state was successfully validated.
    /// </summary>
    Validated,

    /// <summary>
    /// The current authentication state was marked as requiring validation.
    /// </summary>
    MarkedStale,

    /// <summary>
    /// The authentication state was cleared.
    /// </summary>
    Cleared,

    /// <summary>
    /// The state was explicitly touched to request an update or render.
    /// </summary>
    Touched,

    /// <summary>
    /// The current authenticated state was updated without replacing the full snapshot.
    /// </summary>
    Patched
}
