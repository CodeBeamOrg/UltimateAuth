using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserProfileStore<TDbContext> : IUserProfileStore where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly TenantKey _tenant;

    public EfCoreUserProfileStore(TDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    private DbSet<UserProfileProjection> DbSet => _db.Set<UserProfileProjection>();

    public async Task<UserProfile?> GetAsync(UserProfileKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.UserKey == key.UserKey,
                ct);

        return projection?.ToDomain();
    }

    public async Task<bool> ExistsAsync(UserProfileKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await DbSet
            .AnyAsync(x =>
                x.Tenant == _tenant &&
                x.UserKey == key.UserKey,
                ct);
    }

    public async Task AddAsync(UserProfile entity, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = entity.ToProjection();

        if (entity.Version != 0)
            throw new InvalidOperationException("New profile must have version 0.");

        DbSet.Add(projection);

        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(UserProfile entity, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var existing = await DbSet
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.UserKey == entity.UserKey,
                ct);

        if (existing is null)
            throw new UAuthNotFoundException("user_profile_not_found");

        if (existing.Version != expectedVersion)
            throw new UAuthConcurrencyException("user_profile_concurrency_conflict");

        entity.UpdateProjection(existing);
        existing.Version++;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(UserProfileKey key, long expectedVersion, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSet
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.UserKey == key.UserKey,
                ct);

        if (projection is null)
            throw new UAuthNotFoundException("user_profile_not_found");

        if (projection.Version != expectedVersion)
            throw new UAuthConcurrencyException("user_profile_concurrency_conflict");

        if (mode == DeleteMode.Hard)
        {
            DbSet.Remove(projection);
        }
        else
        {
            projection.DeletedAt = now;
            projection.Version++;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<UserProfile>> QueryAsync(UserProfileQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();

        var baseQuery = DbSet
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant);

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

    public async Task<IReadOnlyList<UserProfile>> GetByUsersAsync(IReadOnlyList<UserKey> userKeys, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await DbSet
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant)
            .Where(x => userKeys.Contains(x.UserKey))
            .Where(x => x.DeletedAt == null)
            .ToListAsync(ct);

        return projections.Select(x => x.ToDomain()).ToList();
    }
}
