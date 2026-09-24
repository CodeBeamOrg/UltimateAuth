using CodeBeam.UltimateAuth.Client.Contracts;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

/// <summary>
/// Represents a storage mechanism for client-side data, allowing for setting, retrieving, removing, and checking the existence of key-value pairs within specified storage scopes.
/// </summary>
public interface IClientStorage
{
    /// <summary>
    /// Sets a value in the specified storage scope with the given key.
    /// </summary>
    ValueTask SetAsync(StorageScope scope, string key, string value);

    /// <summary>
    /// Retrieves a value from the specified storage scope using the given key. Returns null if the key does not exist.
    /// </summary>
    ValueTask<string?> GetAsync(StorageScope scope, string key);

    /// <summary>
    /// Removes a value from the specified storage scope using the given key.
    /// </summary>
    ValueTask RemoveAsync(StorageScope scope, string key);

    /// <summary>
    /// Checks if a value exists in the specified storage scope for the given key.
    /// </summary>
    ValueTask<bool> ExistsAsync(StorageScope scope, string key);
}
