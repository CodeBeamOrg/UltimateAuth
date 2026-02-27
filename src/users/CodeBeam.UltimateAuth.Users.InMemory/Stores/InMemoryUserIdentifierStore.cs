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

    public Task<IdentifierExistenceResult> ExistsAsync(IdentifierExistenceQuery query, CancellationToken ct = default)
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

    public Task<UserIdentifier?> GetAsync(TenantKey tenant, UserIdentifierType type, string normalizedValue, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var identifier = _store.Values.FirstOrDefault(x =>
            x.Tenant == tenant &&
            x.Type == type &&
            x.NormalizedValue == normalizedValue &&
            !x.IsDeleted);

        return Task.FromResult(identifier);
    }

    public Task<UserIdentifier?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier))
            return Task.FromResult<UserIdentifier?>(null);

        if (identifier.IsDeleted)
            return Task.FromResult<UserIdentifier?>(null);

        return Task.FromResult<UserIdentifier?>(identifier);
    }

    public Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = _store.Values
            .Where(x => x.Tenant == tenant)
            .Where(x => x.UserKey == userKey)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<UserIdentifier>>(result);
    }

    public Task<PagedResult<UserIdentifier>> QueryAsync(TenantKey tenant, UserIdentifierQuery query, CancellationToken ct = default)
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

    public Task CreateAsync(TenantKey tenant, UserIdentifier identifier, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

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

        _store[identifier.Id] = identifier;

        return Task.CompletedTask;
    }

    public Task UpdateValueAsync(Guid id, string newRawValue, string newNormalizedValue, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier) || identifier.IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (identifier.NormalizedValue == newNormalizedValue)
            throw new UAuthIdentifierConflictException("identifier_value_unchanged");

        identifier.Value = newRawValue;
        identifier.NormalizedValue = newNormalizedValue;

        identifier.IsVerified = false;
        identifier.VerifiedAt = null;
        identifier.UpdatedAt = updatedAt;

        return Task.CompletedTask;
    }

    public Task MarkVerifiedAsync(Guid id, DateTimeOffset verifiedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier) || identifier.IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (identifier.IsVerified)
            throw new UAuthIdentifierConflictException("identifier_already_verified");

        identifier.IsVerified = true;
        identifier.VerifiedAt = verifiedAt;
        identifier.UpdatedAt = verifiedAt;

        return Task.CompletedTask;
    }

    public Task SetPrimaryAsync(Guid id, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var target) || target.IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        foreach (var idf in _store.Values.Where(x =>
                     x.Tenant == target.Tenant &&
                     x.UserKey == target.UserKey &&
                     x.Type == target.Type &&
                     x.IsPrimary &&
                     !x.IsDeleted))
        {
            if (idf.Id == target.Id)
                continue;
            idf.IsPrimary = false;
            idf.UpdatedAt = updatedAt;
        }

        target.IsPrimary = true;
        target.UpdatedAt = updatedAt;

        return Task.CompletedTask;
    }

    public Task UnsetPrimaryAsync(Guid id, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier) || identifier.IsDeleted)
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (!identifier.IsPrimary)
        {
            throw new UAuthIdentifierConflictException("identifier_is_not_primary_already");
        }

        identifier.IsPrimary = false;
        identifier.UpdatedAt = updatedAt;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(id, out var identifier))
            throw new UAuthIdentifierNotFoundException("identifier_not_found");

        if (mode == DeleteMode.Hard)
        {
            _store.Remove(id);
            return Task.CompletedTask;
        }

        if (identifier.IsDeleted)
            throw new UAuthIdentifierConflictException("identifier_already_deleted");

        identifier.IsDeleted = true;
        identifier.DeletedAt = deletedAt;
        identifier.IsPrimary = false;
        identifier.UpdatedAt = deletedAt;

        return Task.CompletedTask;
    }

    public Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var identifiers = _store.Values.Where(x => x.Tenant == tenant && x.UserKey == userKey).ToList();

        foreach (var identifier in identifiers)
        {
            if (mode == DeleteMode.Hard)
            {
                _store.Remove(identifier.Id);
            }
            else
            {
                if (identifier.IsDeleted)
                    continue;

                identifier.IsDeleted = true;
                identifier.DeletedAt = deletedAt;
                identifier.IsPrimary = false;
                identifier.UpdatedAt = deletedAt;
            }
        }

        return Task.CompletedTask;
    }
}
