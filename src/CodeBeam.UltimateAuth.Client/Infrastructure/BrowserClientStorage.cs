using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

public sealed class BrowserClientStorage : IClientStorage
{
    private readonly IJSRuntime _js;

    public BrowserClientStorage(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask SetAsync(StorageScope scope, string key, string value)
        => _js.InvokeVoidAsync("uauth.storage.set", Scope(scope), key, value);

    public ValueTask<string?> GetAsync(StorageScope scope, string key)
        => _js.InvokeAsync<string?>("uauth.storage.get", Scope(scope), key);

    public ValueTask RemoveAsync(StorageScope scope, string key)
        => _js.InvokeVoidAsync("uauth.storage.remove", Scope(scope), key);

    public async ValueTask<bool> ExistsAsync(StorageScope scope, string key)
        => await _js.InvokeAsync<bool>("uauth.storage.exists", Scope(scope), key);

    private static string Scope(StorageScope scope)
        => scope == StorageScope.Local ? "local" : "session";
}
