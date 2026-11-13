using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Stores;

internal sealed class InMemoryAuthStore : IAuthStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, AuthArtifact> _entries = new();
    private readonly IClock _clock;
    private readonly int _maxEntries;

    public InMemoryAuthStore(
        IClock clock,
        IOptions<UAuthServerOptions> options)
    {
        _clock = clock;
        _maxEntries = options.Value.AuthArtifacts.MaxEntries;

        if (_maxEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "AuthArtifacts.MaxEntries must be positive.");
    }

    public Task StoreAsync(AuthArtifactKey key, AuthArtifact artifact, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var now = _clock.UtcNow;

            if (artifact.IsExpired(now))
                throw new ArgumentException("Cannot store an expired auth artifact.", nameof(artifact));

            if (!_entries.ContainsKey(key.Value) && _entries.Count >= _maxEntries)
            {
                RemoveExpiredUnsafe(now);

                if (_entries.Count >= _maxEntries)
                    throw new UAuthArtifactStoreFullException();
            }

            _entries[key.Value] = artifact;
        }

        return Task.CompletedTask;
    }

    public Task<AuthArtifact?> GetAsync(AuthArtifactKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_entries.TryGetValue(key.Value, out var artifact))
                return Task.FromResult<AuthArtifact?>(null);

            if (artifact.IsExpired(_clock.UtcNow))
            {
                _entries.Remove(key.Value);
                return Task.FromResult<AuthArtifact?>(null);
            }

            return Task.FromResult<AuthArtifact?>(artifact);
        }
    }

    public Task<AuthArtifact?> ConsumeAsync(AuthArtifactKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_entries.Remove(key.Value, out var artifact))
                return Task.FromResult<AuthArtifact?>(null);

            if (artifact.IsExpired(_clock.UtcNow))
                return Task.FromResult<AuthArtifact?>(null);

            return Task.FromResult<AuthArtifact?>(artifact);
        }
    }

    /// <summary>
    /// Removes expired artifacts without requiring requests to access them.
    /// </summary>
    internal int CleanupExpired()
    {
        lock (_sync)
        {
            return RemoveExpiredUnsafe(_clock.UtcNow);
        }
    }

    /// <summary>
    /// For deterministic capacity and cleanup tests.
    /// </summary>
    internal int Count
    {
        get
        {
            lock (_sync)
                return _entries.Count;
        }
    }

    // Must be called only while holding _sync.
    private int RemoveExpiredUnsafe(DateTimeOffset now)
    {
        var removed = 0;

        foreach (var entry in _entries.ToArray())
        {
            if (!entry.Value.IsExpired(now))
                continue;

            if (_entries.Remove(entry.Key))
                removed++;
        }

        return removed;
    }
}
