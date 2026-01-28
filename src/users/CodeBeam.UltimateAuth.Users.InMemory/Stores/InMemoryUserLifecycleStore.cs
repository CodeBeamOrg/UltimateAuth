using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserLifecycleStore : IUserLifecycleStore
{
    private readonly Dictionary<(string?, UserKey), UserLifecycle> _store = new();

    public Task<bool> ExistsAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
    {
        return Task.FromResult(_store.TryGetValue((tenantId, userKey), out var entity) && !entity.IsDeleted);
    }

    public Task<UserLifecycle?> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
    {
        if (!_store.TryGetValue((tenantId, userKey), out var entity))
            return Task.FromResult<UserLifecycle?>(null);

        if (entity.IsDeleted)
            return Task.FromResult<UserLifecycle?>(null);

        return Task.FromResult<UserLifecycle?>(entity);
    }

    public Task<PagedResult<UserLifecycle>> QueryAsync(string? tenantId, UserLifecycleQuery query, CancellationToken ct = default)
    {
        var baseQuery = _store.Values
            .Where(x => x?.UserKey != null)
            .Where(x => x.TenantId == tenantId);

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(x => !x.IsDeleted);

        if (query.Status != null)
            baseQuery = baseQuery.Where(x => x.Status == query.Status);

        var totalCount = baseQuery.Count();

        var items = baseQuery
            .OrderBy(x => x.CreatedAt)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(new PagedResult<UserLifecycle>(items, totalCount));
    }

    public Task CreateAsync(string? tenantId, UserLifecycle lifecycle, CancellationToken ct = default)
    {
        var key = (tenantId, lifecycle.UserKey);

        if (_store.ContainsKey(key))
            throw new InvalidOperationException("UserLifecycle already exists.");

        _store[key] = lifecycle;
        return Task.CompletedTask;
    }

    public Task ChangeStatusAsync(string? tenantId, UserKey userKey, UserStatus newStatus, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        if (!_store.TryGetValue((tenantId, userKey), out var entity) || entity.IsDeleted)
            throw new InvalidOperationException("UserLifecycle not found.");

        entity.Status = newStatus;
        entity.UpdatedAt = updatedAt;

        return Task.CompletedTask;
    }

    public Task ChangeSecurityStampAsync(string? tenantId, UserKey userKey, Guid newSecurityStamp, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        if (!_store.TryGetValue((tenantId, userKey), out var entity) || entity.IsDeleted)
            throw new InvalidOperationException("UserLifecycle not found.");

        if (entity.SecurityStamp == newSecurityStamp)
            return Task.CompletedTask;

        entity.SecurityStamp = newSecurityStamp;
        entity.UpdatedAt = updatedAt;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string? tenantId, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        var key = (tenantId, userKey);

        if (!_store.TryGetValue(key, out var entity))
            return Task.CompletedTask;

        if (mode == DeleteMode.Hard)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        // Soft delete (idempotent)
        if (entity.IsDeleted)
            return Task.CompletedTask;

        entity.IsDeleted = true;
        entity.DeletedAt = deletedAt;

        return Task.CompletedTask;
    }
}
