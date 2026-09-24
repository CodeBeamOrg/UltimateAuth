using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

/// <summary>
/// Represents a client storage implementation that uses the browser's localStorage and sessionStorage via JavaScript interop.
/// </summary>
public sealed class BrowserClientStorage : IClientStorage
{
    private readonly IJSRuntime _js;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserClientStorage"/> class with the specified JavaScript runtime.
    /// </summary>
    /// <param name="js"></param>
    public BrowserClientStorage(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Sets a value in the specified storage scope (localStorage or sessionStorage) with the given key.
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public ValueTask SetAsync(StorageScope scope, string key, string value)
        => _js.InvokeVoidAsync("uauth.storage.set", Scope(scope), key, value);

    /// <summary>
    /// Gets a value from the specified storage scope (localStorage or sessionStorage) with the given key.
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public ValueTask<string?> GetAsync(StorageScope scope, string key)
        => _js.InvokeAsync<string?>("uauth.storage.get", Scope(scope), key);

    /// <summary>
    /// Removes a value from the specified storage scope (localStorage or sessionStorage) with the given key.
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public ValueTask RemoveAsync(StorageScope scope, string key)
        => _js.InvokeVoidAsync("uauth.storage.remove", Scope(scope), key);
    
    /// <summary>
    /// Checks if a value exists in the specified storage scope (localStorage or sessionStorage) with the given key.
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public async ValueTask<bool> ExistsAsync(StorageScope scope, string key)
        => await _js.InvokeAsync<bool>("uauth.storage.exists", Scope(scope), key);

    /// <summary>
    /// Gets the string representation of the storage scope for use in JavaScript interop.
    /// </summary>
    /// <param name="scope"></param>
    /// <returns></returns>
    private static string Scope(StorageScope scope)
        => scope == StorageScope.Local ? "local" : "session";
}
