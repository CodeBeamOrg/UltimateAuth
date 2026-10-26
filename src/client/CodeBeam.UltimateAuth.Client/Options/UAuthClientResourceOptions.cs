namespace CodeBeam.UltimateAuth.Client.Options;

/// <summary>
/// Configures client requests to an UltimateAuth-protected Resource API.
/// </summary>
public sealed class UAuthClientResourceOptions
{
    /// <summary>
    /// Base address of the Resource API.
    /// When null, requests use the application's origin.
    /// </summary>
    public string? BaseAddress { get; set; }

    /// <summary>
    /// Controls browser credential transmission.
    /// </summary>
    public UAuthResourceCredentialsMode Credentials { get; set; } = UAuthResourceCredentialsMode.Auto;
}
