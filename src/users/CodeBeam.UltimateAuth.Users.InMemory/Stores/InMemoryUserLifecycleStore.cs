using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserLifecycleStore : InMemoryTenantVersionedStore<UserLifecycle, UserLifecycleKey>, IUserLifecycleStore
{
    protected override UserLifecycleKey GetKey(UserLifecycle entity)
        => new(entity.Tenant, entity.UserKey);

    public InMemoryUserLifecycleStore(TenantContext tenant) : base(tenant)
    {
    }

    public Task<PagedResult<UserLifecycle>> QueryAsync(UserLifecycleQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();
        var baseQuery = TenantValues().AsQueryable();

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(x => !x.IsDeleted);

        if (query.Status != null)
            baseQuery = baseQuery.Where(x => x.Status == query.Status);

        baseQuery = query.SortBy switch
        {
            nameof(UserLifecycle.Id) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.Id)
                    : baseQuery.OrderBy(x => x.Id),

            nameof(UserLifecycle.CreatedAt) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.CreatedAt)
                    : baseQuery.OrderBy(x => x.CreatedAt),

            nameof(UserLifecycle.Status) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.Status)
                    : baseQuery.OrderBy(x => x.Status),

            nameof(UserLifecycle.UserKey) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.UserKey.Value)
                    : baseQuery.OrderBy(x => x.UserKey.Value),

            nameof(UserLifecycle.DeletedAt) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.DeletedAt)
                    : baseQuery.OrderBy(x => x.DeletedAt),

            _ => baseQuery.OrderBy(x => x.CreatedAt)
        };

        var totalCount = baseQuery.Count();
        var items = baseQuery.Skip((normalized.PageNumber - 1) * normalized.PageSize).Take(normalized.PageSize).Select(x => x.Snapshot()).ToList().AsReadOnly();

        return Task.FromResult(new PagedResult<UserLifecycle>(items, totalCount, normalized.PageNumber, normalized.PageSize, query.SortBy, query.Descending));
    }
}
