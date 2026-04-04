using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserIdentifierStore : InMemoryTenantVersionedStore<UserIdentifier, Guid>, IUserIdentifierStore
{
    protected override Guid GetKey(UserIdentifier entity) => entity.Id;
    private readonly object _primaryLock = new();

    public InMemoryUserIdentifierStore(TenantContext tenant) : base(tenant)
    {

    }

    public Task<IdentifierExistenceResult> ExistsAsync(IdentifierExistenceQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var candidates = TenantValues()
            .Where(x =>
                x.Type == query.Type &&
                x.NormalizedValue == query.NormalizedValue &&
                !x.IsDeleted);

        if (query.ExcludeIdentifierId.HasValue)
            candidates = candidates.Where(x => x.Id != query.ExcludeIdentifierId.Value);

        candidates = query.Scope switch
        {
            IdentifierExistenceScope.WithinUser =>
                candidates.Where(x => x.UserKey == query.UserKey),

            IdentifierExistenceScope.TenantPrimaryOnly =>
                candidates.Where(x => x.IsPrimary),

            IdentifierExistenceScope.TenantAny =>
                candidates,

            _ => candidates
        };

        var match = candidates.FirstOrDefault();

        if (match is null)
            return Task.FromResult(new IdentifierExistenceResult(false));

        return Task.FromResult(
            new IdentifierExistenceResult(true, match.UserKey, match.Id, match.IsPrimary));
    }

    public Task<UserIdentifier?> GetAsync(UserIdentifierType type, string normalizedValue, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var identifier = TenantValues()
            .FirstOrDefault(x =>
                x.Type == type &&
                x.NormalizedValue == normalizedValue &&
                !x.IsDeleted);

        return Task.FromResult(identifier?.Snapshot());
    }

    public Task<UserIdentifier?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return GetAsync(id, ct);
    }

    public Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = TenantValues()
            .Where(x => x.UserKey == userKey)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<UserIdentifier>>(result);
    }

    protected override void BeforeAdd(UserIdentifier entity)
    {
        if (!entity.IsPrimary)
            return;

        foreach (var other in InternalValues().Where(x =>
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

    public override Task AddAsync(UserIdentifier entity, CancellationToken ct = default)
    {
        lock (_primaryLock)
        {
            return base.AddAsync(entity, ct);
        }
    }

    protected override void BeforeSave(UserIdentifier entity, UserIdentifier current, long expectedVersion)
    {
        if (!entity.IsPrimary)
            return;

        foreach (var other in InternalValues().Where(x =>
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

    public override Task SaveAsync(UserIdentifier entity, long expectedVersion, CancellationToken ct = default)
    {
        lock (_primaryLock)
        {
            return base.SaveAsync(entity, expectedVersion, ct);
        }
    }

    public Task<PagedResult<UserIdentifier>> QueryAsync(UserIdentifierQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (query.UserKey is null)
            throw new UAuthIdentifierValidationException("userKey_required");

        var normalized = query.Normalize();

        var baseQuery = TenantValues()
            .Where(x => x.UserKey == query.UserKey.Value);

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(x => !x.IsDeleted);

        baseQuery = query.SortBy switch
        {
            nameof(UserIdentifier.Type) => query.Descending
                ? baseQuery.OrderByDescending(x => x.Type)
                : baseQuery.OrderBy(x => x.Type),

            nameof(UserIdentifier.CreatedAt) => query.Descending
                ? baseQuery.OrderByDescending(x => x.CreatedAt)
                : baseQuery.OrderBy(x => x.CreatedAt),

            nameof(UserIdentifier.UpdatedAt) => query.Descending
                ? baseQuery.OrderByDescending(x => x.UpdatedAt)
                : baseQuery.OrderBy(x => x.UpdatedAt),

            nameof(UserIdentifier.Value) => query.Descending
                ? baseQuery.OrderByDescending(x => x.Value)
                : baseQuery.OrderBy(x => x.Value),

            nameof(UserIdentifier.NormalizedValue) => query.Descending
                ? baseQuery.OrderByDescending(x => x.NormalizedValue)
                : baseQuery.OrderBy(x => x.NormalizedValue),

            _ => baseQuery.OrderBy(x => x.CreatedAt)
        };

        var totalCount = baseQuery.Count();

        var items = baseQuery
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(x => x.Snapshot())
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

    public Task<IReadOnlyList<UserIdentifier>> GetByUsersAsync(IReadOnlyList<UserKey> userKeys, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var set = userKeys.ToHashSet();

        var result = TenantValues()
            .Where(x => set.Contains(x.UserKey))
            .Where(x => !x.IsDeleted)
            .Select(x => x.Snapshot())
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<UserIdentifier>>(result);
    }

    public async Task DeleteByUserAsync(UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var identifiers = TenantValues()
            .Where(x => x.UserKey == userKey && !x.IsDeleted)
            .ToList();

        foreach (var identifier in identifiers)
        {
            await DeleteAsync(identifier.Id, identifier.Version, mode, deletedAt, ct);
        }
    }
}
