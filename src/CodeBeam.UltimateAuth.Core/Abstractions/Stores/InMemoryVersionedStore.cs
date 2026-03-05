using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public abstract class InMemoryVersionedStore<TEntity, TKey> where TEntity : class, IVersionedEntity where TKey : notnull, IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, TEntity> _store = new();

    protected abstract TKey GetKey(TEntity entity);

    public Task<TEntity?> GetAsync(TKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _store.TryGetValue(key, out var entity);
        return Task.FromResult(entity);
    }

    public Task<bool> ExistsAsync(TKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(_store.ContainsKey(key));
    }

    public Task CreateAsync(TEntity entity, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = GetKey(entity);

        if (!_store.TryAdd(key, entity))
            throw new InvalidOperationException($"{typeof(TEntity).Name} already exists.");

        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = GetKey(entity);

        if (!_store.TryGetValue(key, out var current))
            throw new InvalidOperationException($"{typeof(TEntity).Name} not found.");

        if (current.Version != expectedVersion)
            throw new InvalidOperationException($"{typeof(TEntity).Name} version conflict.");

        entity.Version++;

        if (!_store.TryUpdate(key, entity, current))
            throw new InvalidOperationException($"{typeof(TEntity).Name} update conflict.");

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TKey key, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(key, out var current))
            return Task.CompletedTask;

        if (mode == DeleteMode.Hard)
        {
            _store.TryRemove(key, out _);
            return Task.CompletedTask;
        };

        var original = current;
        if (current is ISoftDeletable soft)
        {
            if (soft.IsDeleted)
                return Task.CompletedTask;

            soft.MarkDeleted(now);
            current.Version++;

            if (!_store.TryUpdate(key, current, original))
                throw new UAuthConflictException("Delete conflict");
        }

        return Task.CompletedTask;
    }

    protected IEnumerable<TEntity> Values => _store.Values;

    protected bool TryGet(TKey key, out TEntity? entity) => _store.TryGetValue(key, out entity);
}
