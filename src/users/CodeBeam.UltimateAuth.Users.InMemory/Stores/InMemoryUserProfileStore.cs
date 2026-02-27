using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserProfileStore : IUserProfileStore
{
    private readonly Dictionary<(TenantKey Tenant, UserKey UserKey), UserProfile> _store = new();

    public Task<bool> ExistsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        return Task.FromResult(_store.TryGetValue((tenant, userKey), out var profile) && profile.DeletedAt == null);
    }

    public Task<UserProfile?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        if (!_store.TryGetValue((tenant, userKey), out var profile))
            return Task.FromResult<UserProfile?>(null);

        if (profile.DeletedAt != null)
            return Task.FromResult<UserProfile?>(null);

        return Task.FromResult<UserProfile?>(profile);
    }

    public Task<PagedResult<UserProfile>> QueryAsync(TenantKey tenant, UserProfileQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();

        var baseQuery = _store.Values
            .Where(x => x.Tenant == tenant);

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(x => x.DeletedAt == null);

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

            _ => baseQuery.OrderBy(x => x.CreatedAt)
        };

        var totalCount = baseQuery.Count();
        var items = baseQuery.Skip((normalized.PageNumber - 1) * normalized.PageSize).Take(normalized.PageSize).ToList().AsReadOnly();

        return Task.FromResult(new PagedResult<UserProfile>(items, totalCount, normalized.PageNumber, normalized.PageSize, query.SortBy, query.Descending));
    }

    public Task CreateAsync(TenantKey tenant, UserProfile profile, CancellationToken ct = default)
    {
        var key = (tenant, profile.UserKey);

        if (_store.ContainsKey(key))
            throw new InvalidOperationException("UserProfile already exists.");

        _store[key] = profile;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TenantKey tenant, UserKey userKey, UserProfileUpdate update, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        var key = (tenant, userKey);

        if (!_store.TryGetValue(key, out var existing) || existing.DeletedAt != null)
            throw new InvalidOperationException("UserProfile not found.");

        existing.FirstName = update.FirstName;
        existing.LastName = update.LastName;
        existing.DisplayName = update.DisplayName;
        existing.BirthDate = update.BirthDate;
        existing.Gender = update.Gender;
        existing.Bio = update.Bio;
        existing.Language = update.Language;
        existing.TimeZone = update.TimeZone;
        existing.Culture = update.Culture;
        existing.Metadata = update.Metadata;

        existing.UpdatedAt = updatedAt;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        var key = (tenant, userKey);

        if (!_store.TryGetValue(key, out var profile))
            return Task.CompletedTask;

        if (mode == DeleteMode.Hard)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        if (profile.IsDeleted)
            return Task.CompletedTask;

        profile.IsDeleted = true;
        profile.DeletedAt = deletedAt;

        return Task.CompletedTask;
    }
}
