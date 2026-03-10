using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

internal sealed class InMemoryRoleStore : InMemoryVersionedStore<Role, RoleKey>, IRoleStore
{
    protected override RoleKey GetKey(Role entity) => new(entity.Tenant, entity.Id);

    protected override void BeforeAdd(Role entity)
    {
        if (Values().Any(r =>
                r.Tenant == entity.Tenant &&
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
            if (Values().Any(r =>
                    r.Tenant == entity.Tenant &&
                    r.NormalizedName == entity.NormalizedName &&
                    r.Id != entity.Id &&
                    !r.IsDeleted))
            {
                throw new UAuthConflictException("role_name_already_exists");
            }
        }
    }

    public Task<Role?> GetByNameAsync(TenantKey tenant, string normalizedName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var role = Values()
            .FirstOrDefault(r =>
                r.Tenant == tenant &&
                r.NormalizedName == normalizedName &&
                !r.IsDeleted);

        return Task.FromResult(role);
    }

    public Task<IReadOnlyCollection<Role>> GetByIdsAsync(TenantKey tenant, IReadOnlyCollection<RoleId> roleIds, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = new List<Role>(roleIds.Count);

        foreach (var id in roleIds)
        {
            if (TryGet(new RoleKey(tenant, id), out var role) && role is not null)
            {
                result.Add(role.Snapshot());
            }
        }

        return Task.FromResult<IReadOnlyCollection<Role>>(result);
    }

    public Task<PagedResult<Role>> QueryAsync(TenantKey tenant, RoleQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();

        var baseQuery = Values()
            .Where(r => r.Tenant == tenant);

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