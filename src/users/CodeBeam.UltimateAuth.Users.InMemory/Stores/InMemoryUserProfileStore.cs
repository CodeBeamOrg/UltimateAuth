using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserProfileStore : InMemoryTenantVersionedStore<UserProfile, UserProfileKey>, IUserProfileStore
{
    protected override UserProfileKey GetKey(UserProfile entity)
        => new(entity.Tenant, entity.UserKey, entity.ProfileKey);

    public InMemoryUserProfileStore(TenantContext tenant) : base(tenant)
    {
    }

    protected override void BeforeAdd(UserProfile entity)
    {
        var exists = TenantValues()
            .Any(x =>
                x.UserKey == entity.UserKey &&
                x.ProfileKey == entity.ProfileKey);

        if (exists)
            throw new UAuthConflictException("profile_already_exists");
    }

    public Task<PagedResult<UserProfile>> QueryAsync(UserProfileQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();
        var baseQuery = TenantValues().AsQueryable();

        if (query.ProfileKey != null)
        {
            baseQuery = baseQuery.Where(x => x.ProfileKey == query.ProfileKey);
        }

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
            .Select(x => x.Snapshot())
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

    public Task<IReadOnlyList<UserProfile>> GetByUsersAsync(IReadOnlyList<UserKey> userKeys, ProfileKey profileKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var set = userKeys.ToHashSet();

        var query = TenantValues()
            .Where(x => set.Contains(x.UserKey))
            .Where(x => x.ProfileKey == profileKey)
            .Where(x => !x.IsDeleted);

        var result = query
            .Select(x => x.Snapshot())
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<UserProfile>>(result);
    }

    public Task<IReadOnlyList<UserProfile>> GetAllProfilesByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var query = TenantValues()
            .Where(x => x.UserKey == userKey)
            .Where(x => !x.IsDeleted);
        var result = query
            .Select(x => x.Snapshot())
            .ToList()
            .AsReadOnly();
        return Task.FromResult<IReadOnlyList<UserProfile>>(result);
    }
}