using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserProfileStore : InMemoryVersionedStore<UserProfile, UserProfileKey>, IUserProfileStore
{
    protected override UserProfileKey GetKey(UserProfile entity)
        => new(entity.Tenant, entity.UserKey);

    public Task<PagedResult<UserProfile>> QueryAsync(TenantKey tenant, UserProfileQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();

        var baseQuery = Values()
            .Where(x => x.Tenant == tenant);

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(x => !x.IsDeleted);

        baseQuery = query.SortBy switch
        {
            nameof(UserProfile.CreatedAt) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.CreatedAt)
                    : baseQuery.OrderBy(x => x.CreatedAt),

            nameof(UserProfile.DisplayName) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.DisplayName)
                    : baseQuery.OrderBy(x => x.DisplayName),

            nameof(UserProfile.FirstName) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.FirstName)
                    : baseQuery.OrderBy(x => x.FirstName),

            nameof(UserProfile.LastName) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.LastName)
                    : baseQuery.OrderBy(x => x.LastName),

            _ => baseQuery.OrderBy(x => x.CreatedAt)
        };

        var totalCount = baseQuery.Count();

        var items = baseQuery
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(
            new PagedResult<UserProfile>(
                items,
                totalCount,
                normalized.PageNumber,
                normalized.PageSize,
                query.SortBy,
                query.Descending));
    }
}