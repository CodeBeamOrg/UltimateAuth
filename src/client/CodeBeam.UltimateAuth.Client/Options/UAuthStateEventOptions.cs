namespace CodeBeam.UltimateAuth.Client.Options;

/// <summary>
/// Options for handling UAuth state events in the client.
/// </summary>
public class UAuthStateEventOptions
{
    /// <summary>
    /// Gets or sets the handling mode for UAuth state events.
    /// </summary>
    public UAuthStateEventHandlingMode HandlingMode { get; set; } = UAuthStateEventHandlingMode.Patch;
}
