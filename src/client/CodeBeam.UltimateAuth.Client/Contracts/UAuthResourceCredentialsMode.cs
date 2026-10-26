namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Controls browser credential transmission for resource requests.
/// </summary>
public enum UAuthResourceCredentialsMode
{
    /// <summary>
    /// Automatically selects the appropriate browser credentials policy.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Send browser credentials only for same-origin requests.
    /// </summary>
    SameOrigin = 10,

    /// <summary>
    /// Include browser credentials for the configured resource origin.
    /// </summary>
    Include = 20,

    /// <summary>
    /// Do not send browser credentials.
    /// </summary>
    Omit = 30
}
