using CodeBeam.UltimateAuth.Client.Contracts;

namespace CodeBeam.UltimateAuth.Client.Utilities
{
    public interface IBrowserStorage
    {
        ValueTask SetAsync(StorageScope scope, string key, string value);
        ValueTask<string?> GetAsync(StorageScope scope, string key);
        ValueTask RemoveAsync(StorageScope scope, string key);
        ValueTask<bool> ExistsAsync(StorageScope scope, string key);
    }
}
