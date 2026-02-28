using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserIdentifierStore : IUserIdentifierStore
{
    private readonly Dictionary<Guid, UserIdentifier> _store = new();
    private readonly object _lock = new();

    public Task<IdentifierExistenceResult> ExistsAsync(IdentifierExistenceQuery query, CancellationToken ct = default)
    {
        lock (_lock)
        {
            ct.ThrowIfCancellationRequested();

            var candidates = _store.Values
                .Where(x =>
                    x.Tenant == query.Tenant &&
                    x.Type == query.Type &&
                    x.NormalizedValue == query.NormalizedValue &&
                    !x.IsDeleted);

            if (query.ExcludeIdentifierId.HasValue)
                candidates = candidates.Where(x => x.Id != query.ExcludeIdentifierId.Value);

            candidates = query.Scope switch
            {
                IdentifierExistenceScope.WithinUser => candidates.Where(x => x.UserKey == query.UserKey),
                IdentifierExistenceScope.TenantPrimaryOnly => candidates.Where(x => x.IsPrimary),
                IdentifierExistenceScope.TenantAny => candidates,
                _ => candidates
            };

            var match = candidates.FirstOrDefault();

            if (match is null)
                return Task.FromResult(new IdentifierExistenceResult(false));

            return Task.FromResult(new IdentifierExistenceResult(true, match.UserKey, match.Id, match.IsPrimary));
        }
    }

    public Task<UserIdentifier?> GetAsync(TenantKey tenant, UserIdentifierType type, string normalizedValue, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var identifier = _store.Values.FirstOrDefault(x =>
            x.Tenant == tenant &&
            x.Type == type &&
            x.NormalizedValue == normalizedValue &&
            !x.IsDeleted);

            return Task.FromResult(identifier?.Cloned());
        }
    }

    public Task<UserIdentifier?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_store.TryGetValue(id, out var identifier))
                return Task.FromResult<UserIdentifier?>(null);

            if (identifier.IsDeleted)
                return Task.FromResult<UserIdentifier?>(null);

            return Task.FromResult<UserIdentifier?>(identifier.Cloned());
        }
    }

    public Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var result = _store.Values
                .Where(x => x.Tenant == tenant)
                .Where(x => x.UserKey == userKey)
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.Cloned())
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<UserIdentifier>>(result);
        }
    }

    public Task<PagedResult<UserIdentifier>> QueryAsync(TenantKey tenant, UserIdentifierQuery query, CancellationToken ct = default)
    {
        lock (_lock)
        {
            ct.ThrowIfCancellationRequested();

            if (query.UserKey is null)
                throw new UAuthIdentifierValidationException("userKey_required");

            var normalized = query.Normalize();

            var baseQuery = _store.Values
                .Where(x => x.Tenant == tenant)
                .Where(x => x.UserKey == query.UserKey.Value);

            if (!query.IncludeDeleted)
                baseQuery = baseQuery.Where(x => !x.IsDeleted);

            baseQuery = query.SortBy switch
            {
                nameof(UserIdentifier.Type) =>
                    query.Descending ? baseQuery.OrderByDescending(x => x.Type)
                                     : baseQuery.OrderBy(x => x.Type),

                nameof(UserIdentifier.CreatedAt) =>
                    query.Descending ? baseQuery.OrderByDescending(x => x.CreatedAt)
                                     : baseQuery.OrderBy(x => x.CreatedAt),

                _ => baseQuery.OrderBy(x => x.CreatedAt)
            };

            var totalCount = baseQuery.Count();

            var items = baseQuery
                .Skip((normalized.PageNumber - 1) * normalized.PageSize)
                .Take(normalized.PageSize)
                .Select(x => x.Cloned())
                .ToList()
                .AsReadOnly();

            return Task.FromResult(
                new PagedResult<UserIdentifier>(
                    items,
                    totalCount,
                    normalized.PageNumber,
                    normalized.PageSize,
                    query.SortBy,
                    query.Descending));
        }
    }

    public Task SaveAsync(UserIdentifier entity, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_store.TryGetValue(entity.Id, out var existing))
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            if (existing.Version != expectedVersion)
                throw new UAuthConcurrencyException("identifier_concurrency_conflict");

            if (entity.IsPrimary)
            {
                foreach (var other in _store.Values.Where(x =>
                             x.Tenant == entity.Tenant &&
                             x.UserKey == entity.UserKey &&
                             x.Type == entity.Type &&
                             x.Id != entity.Id &&
                             x.IsPrimary &&
                             !x.IsDeleted))
                {
                    other.UnsetPrimary(entity.UpdatedAt ?? entity.CreatedAt);
                }
            }

            _store[entity.Id] = entity.Cloned();
        }

        return Task.CompletedTask;
    }

    public Task CreateAsync(TenantKey tenant, UserIdentifier identifier, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (identifier.Id == Guid.Empty)
                identifier.Id = Guid.NewGuid();

            identifier.Tenant = tenant;


            if (identifier.IsPrimary)
            {
                foreach (var existing in _store.Values.Where(x =>
                             x.Tenant == tenant &&
                             x.UserKey == identifier.UserKey &&
                             x.Type == identifier.Type &&
                             x.IsPrimary &&
                             !x.IsDeleted))
                {
                    existing.IsPrimary = false;
                    existing.UpdatedAt = identifier.CreatedAt;
                }
            }

            identifier.Version = 0;
            _store[identifier.Id] = identifier;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserIdentifier entity, long expectedVersion, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_store.TryGetValue(entity.Id, out var identifier))
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            if (identifier.Version != expectedVersion)
                throw new UAuthConcurrencyException("identifier_concurrency_conflict");

            if (mode == DeleteMode.Hard)
            {
                _store.Remove(entity.Id);
                return Task.CompletedTask;
            }

            _store[entity.Id] = entity.Cloned();
        }
        return Task.CompletedTask;
    }

    public async Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        List<UserIdentifier> snapshot;

        lock (_lock)
        {
            snapshot = _store.Values
                .Where(x => x.Tenant == tenant && x.UserKey == userKey)
                .Select(x => x.Cloned())
                .ToList();
        }

        foreach (var identifier in snapshot)
        {
            if (mode == DeleteMode.Hard)
            {
                lock (_lock)
                {
                    _store.Remove(identifier.Id);
                }
            }
            else
            {
                var expected = identifier.Version;
                identifier.SoftDelete(deletedAt);
                await SaveAsync(identifier, expected, ct);
            }
        }
    }
}
