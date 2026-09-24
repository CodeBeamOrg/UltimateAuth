namespace CodeBeam.UltimateAuth.Client.Contracts;

/// <summary>
/// Specifies the browser storage scope used for UltimateAuth client data.
/// </summary>
public enum StorageScope
{
    /// <summary>
    /// Stores data in storage scoped to the current browser session.
    /// </summary>
    Session = 0,

    /// <summary>
    /// Stores data in persistent browser-local storage.
    /// </summary>
    Local = 10
}
