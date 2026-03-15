using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserProfileStore : IUserProfileStore
{
    private readonly UAuthUserDbContext _db;

    public EfCoreUserProfileStore(UAuthUserDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfile?> GetAsync(UserProfileKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Tenant == key.Tenant &&
                x.UserKey == key.UserKey,
                ct);

        return projection?.ToDomain();
    }

    public async Task<bool> ExistsAsync(UserProfileKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await _db.Profiles
            .AnyAsync(x =>
                x.Tenant == key.Tenant &&
                x.UserKey == key.UserKey,
                ct);
    }

    public async Task AddAsync(UserProfile entity, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = entity.ToProjection();
        _db.Profiles.Add(projection);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(UserProfile entity, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = entity.ToProjection();

        if (entity.Version != expectedVersion + 1)
            throw new InvalidOperationException("Profile version must be incremented by domain.");

        _db.Entry(projection).State = EntityState.Modified;
        _db.Entry(projection).Property(x => x.Version).OriginalValue = expectedVersion;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(UserProfileKey key, long expectedVersion, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Profiles
            .SingleOrDefaultAsync(x =>
                x.Tenant == key.Tenant &&
                x.UserKey == key.UserKey,
                ct);

        if (projection is null)
            throw new UAuthNotFoundException("user_profile_not_found");

        if (projection.Version != expectedVersion)
            throw new UAuthConcurrencyException("user_profile_concurrency_conflict");

        if (mode == DeleteMode.Hard)
        {
            _db.Profiles.Remove(projection);
        }
        else
        {
            projection.DeletedAt = now;
            projection.Version++;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<UserProfile>> QueryAsync(TenantKey tenant, UserProfileQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();

        var baseQuery = _db.Profiles
            .AsNoTracking()
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

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserProfile>(
            items.Select(x => x.ToDomain()).ToList(),
            total,
            normalized.PageNumber,
            normalized.PageSize,
            query.SortBy,
            query.Descending);
    }

    public async Task<IReadOnlyList<UserProfile>> GetByUsersAsync(TenantKey tenant, IReadOnlyList<UserKey> userKeys, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await _db.Profiles
            .AsNoTracking()
            .Where(x => x.Tenant == tenant)
            .Where(x => userKeys.Contains(x.UserKey))
            .Where(x => x.DeletedAt == null)
            .ToListAsync(ct);

        return projections.Select(x => x.ToDomain()).ToList();
    }
}
