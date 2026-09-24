namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Specifies how UltimateAuth authentication state changes affect UI rendering.
/// </summary>
public enum UAuthRenderMode
{
    /// <summary>
    /// Does not automatically request a UI re-render in response to authentication
    /// state change notifications.
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Automatically requests a UI re-render in response to authentication state change notifications.
    /// </summary>
    Reactive = 10
}
