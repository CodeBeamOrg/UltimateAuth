using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

/// <summary>
/// Provides authenticated HTTP operations for application resources.
/// </summary>
public interface IResourceClient
{
    Task<UAuthResult<T>> GetAsync<T>(string endpoint, CancellationToken ct = default);

    Task<UAuthResult<T>> PostAsync<T>(string endpoint, object? request, CancellationToken ct = default);

    Task<UAuthResult<T>> PutAsync<T>(string endpoint, object? request, CancellationToken ct = default);

    Task<UAuthResult<T>> PatchAsync<T>(string endpoint, object? request, CancellationToken ct = default);

    Task<UAuthResult> DeleteAsync(string endpoint, CancellationToken ct = default);
}
