using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

// TODO: Add IQueryStore
public interface IVersionedStore<TEntity, TKey>
{
    Task<TEntity?> GetAsync(TKey key, CancellationToken ct = default);

    Task<bool> ExistsAsync(TKey key, CancellationToken ct = default);

    Task CreateAsync(TEntity entity, CancellationToken ct = default);

    Task UpdateAsync(TEntity entity, long expectedVersion, CancellationToken ct = default);

    Task DeleteAsync(TKey key, DeleteMode deleteMode, DateTimeOffset now, CancellationToken ct = default);
}
