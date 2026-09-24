using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Abstractions;

/// <summary>
/// Provides a mechanism to retrieve the device context for the client application.
/// </summary>
public interface IClientDeviceProvider
{
    /// <summary>
    /// Retrieves the device context for the client application asynchronously.
    /// </summary>
    /// <returns></returns>
    Task<DeviceContext> GetAsync();
}
