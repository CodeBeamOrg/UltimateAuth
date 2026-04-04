using CodeBeam.UltimateAuth.Core.Domain;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Server.Stores;

internal sealed class InMemoryAuthStore : IAuthStore
{
    private sealed record Entry(AuthArtifact Artifact);

    private readonly ConcurrentDictionary<string, Entry> _store = new();

    public Task StoreAsync(AuthArtifactKey key, AuthArtifact artifact, CancellationToken cancellationToken = default)
    {
        _store[key.Value] = new Entry(artifact);
        return Task.CompletedTask;
    }

    public Task<AuthArtifact?> GetAsync(AuthArtifactKey key, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(key.Value, out var entry))
            return Task.FromResult<AuthArtifact?>(null);

        if (entry.Artifact.IsExpired(DateTimeOffset.UtcNow))
        {
            _store.TryRemove(key.Value, out _);
            return Task.FromResult<AuthArtifact?>(null);
        }

        return Task.FromResult<AuthArtifact?>(entry.Artifact);
    }

    public Task<AuthArtifact?> ConsumeAsync(AuthArtifactKey key, CancellationToken cancellationToken = default)
    {
        if (!_store.TryRemove(key.Value, out var entry))
            return Task.FromResult<AuthArtifact?>(null);

        if (entry.Artifact.IsExpired(DateTimeOffset.UtcNow))
            return Task.FromResult<AuthArtifact?>(null);

        return Task.FromResult<AuthArtifact?>(entry.Artifact);
    }
}
