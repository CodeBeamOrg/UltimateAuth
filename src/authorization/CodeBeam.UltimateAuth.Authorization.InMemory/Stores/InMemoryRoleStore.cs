using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.InMemory;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

internal sealed class InMemoryRoleStore : InMemoryTenantVersionedStore<Role, RoleKey>, IRoleStore
{
    protected override RoleKey GetKey(Role entity) => new(entity.Tenant, entity.Id);

    public InMemoryRoleStore(TenantContext tenant) : base(tenant)
    {
    }

    protected override void BeforeAdd(Role entity)
    {
        if (TenantValues().Any(r =>
                r.NormalizedName == entity.NormalizedName &&
                !r.IsDeleted))
        {
            throw new UAuthConflictException("role_already_exists");
        }
    }

    protected override void BeforeSave(Role entity, Role current, long expectedVersion)
    {
        if (entity.NormalizedName != current.NormalizedName)
        {
            if (TenantValues().Any(r =>
                    r.NormalizedName == entity.NormalizedName &&
                    r.Id != entity.Id &&
                    !r.IsDeleted))
            {
                throw new UAuthConflictException("role_name_already_exists");
            }
        }
    }

    public Task<Role?> GetByNameAsync(string normalizedName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var role = TenantValues()
            .FirstOrDefault(r =>
                r.NormalizedName == normalizedName &&
                !r.IsDeleted);

        return Task.FromResult(role?.Snapshot());
    }

    public Task<IReadOnlyCollection<Role>> GetByIdsAsync(IReadOnlyCollection<RoleId> roleIds, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var set = roleIds.ToHashSet();

        var result = TenantValues()
            .Where(r => set.Contains(r.Id) && !r.IsDeleted)
            .Select(r => r.Snapshot())
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyCollection<Role>>(result);
    }

    public Task<PagedResult<Role>> QueryAsync(RoleQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();

        var baseQuery = TenantValues().AsQueryable();

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();

            baseQuery = baseQuery.Where(r =>
                r.NormalizedName.Contains(search));
        }

        baseQuery = query.SortBy switch
        {
            nameof(Role.CreatedAt) => query.Descending
                ? baseQuery.OrderByDescending(x => x.CreatedAt)
                : baseQuery.OrderBy(x => x.CreatedAt),

            nameof(Role.UpdatedAt) => query.Descending
                ? baseQuery.OrderByDescending(x => x.UpdatedAt)
                : baseQuery.OrderBy(x => x.UpdatedAt),

            nameof(Role.Name) => query.Descending
                ? baseQuery.OrderByDescending(x => x.Name)
                : baseQuery.OrderBy(x => x.Name),

            nameof(Role.NormalizedName) => query.Descending
                ? baseQuery.OrderByDescending(x => x.NormalizedName)
                : baseQuery.OrderBy(x => x.NormalizedName),

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
            new PagedResult<Role>(
                items,
                totalCount,
                normalized.PageNumber,
                normalized.PageSize,
                query.SortBy,
                query.Descending));
    }
}