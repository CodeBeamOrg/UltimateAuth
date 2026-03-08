using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public abstract class InMemoryVersionedStore<TEntity, TKey> : IVersionedStore<TEntity, TKey>
    where TEntity : class, IVersionedEntity, IEntitySnapshot<TEntity>
    where TKey : notnull, IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, TEntity> _store = new();

    protected abstract TKey GetKey(TEntity entity);
    protected virtual TEntity Snapshot(TEntity entity) => entity.Snapshot();
    protected virtual void BeforeAdd(TEntity entity) { }
    protected virtual void BeforeSave(TEntity entity, TEntity current, long expectedVersion) { }
    protected virtual void BeforeDelete(TEntity current, long expectedVersion, DeleteMode mode, DateTimeOffset now) { }

    public Task<TEntity?> GetAsync(TKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(key, out var entity))
            return Task.FromResult<TEntity?>(null);

        return Task.FromResult<TEntity?>(Snapshot(entity));
    }

    public Task<bool> ExistsAsync(TKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(_store.ContainsKey(key));
    }

    public Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = GetKey(entity);
        var snapshot = Snapshot(entity);

        BeforeAdd(snapshot);

        if (!_store.TryAdd(key, snapshot))
            throw new UAuthConflictException($"{typeof(TEntity).Name} already exists.");

        return Task.CompletedTask;
    }

    public Task SaveAsync(TEntity entity, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = GetKey(entity);

        if (!_store.TryGetValue(key, out var current))
            throw new UAuthNotFoundException($"{typeof(TEntity).Name} not found.");

        if (current.Version != expectedVersion)
            throw new UAuthConcurrencyException($"{typeof(TEntity).Name} version conflict.");

        var next = Snapshot(entity);
        next.Version = expectedVersion + 1;

        BeforeSave(next, current, expectedVersion);

        if (!_store.TryUpdate(key, next, current))
            throw new UAuthConcurrencyException($"{typeof(TEntity).Name} update conflict.");

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TKey key, long expectedVersion, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(key, out var current))
            throw new UAuthNotFoundException($"{typeof(TEntity).Name} not found.");

        if (current.Version != expectedVersion)
            throw new UAuthConcurrencyException($"{typeof(TEntity).Name} version conflict.");

        BeforeDelete(current, expectedVersion, mode, now);

        if (mode == DeleteMode.Hard)
        {
            if (!_store.TryRemove(new KeyValuePair<TKey, TEntity>(key, current)))
                throw new UAuthConcurrencyException($"{typeof(TEntity).Name} delete conflict.");

            return Task.CompletedTask;
        }

        var next = Snapshot(current);

        if (next is not ISoftDeletable<TEntity> soft)
            throw new UAuthConflictException($"{typeof(TEntity).Name} does not support soft delete.");

        next = soft.MarkDeleted(now);
        next.Version = expectedVersion + 1;

        if (!_store.TryUpdate(key, next, current))
            throw new UAuthConcurrencyException($"{typeof(TEntity).Name} delete conflict.");

        return Task.CompletedTask;
    }

    protected IReadOnlyList<TEntity> Values() => _store.Values.Select(Snapshot).ToList().AsReadOnly();

    protected bool TryGet(TKey key, out TEntity? entity) => _store.TryGetValue(key, out entity);
}
