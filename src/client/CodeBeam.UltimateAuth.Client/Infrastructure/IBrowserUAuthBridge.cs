namespace CodeBeam.UltimateAuth.Client.Infrastructure;

/// <summary>
/// Represents a bridge for browser-specific operations in the UltimateAuth client.
/// </summary>
public interface IBrowserUAuthBridge
{
    /// <summary>
    /// Sets the device ID in the browser's local storage or cookies.
    /// </summary>
    ValueTask SetDeviceIdAsync(string deviceId);
}
