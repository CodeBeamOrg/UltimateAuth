using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserLifecycleStore<TDbContext> : IUserLifecycleStore where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly TenantKey _tenant;

    public EfCoreUserLifecycleStore(TDbContext db, TenantExecutionContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    private DbSet<UserLifecycleProjection> DbSet => _db.Set<UserLifecycleProjection>();

    public async Task<UserLifecycle?> GetAsync(UserLifecycleKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Tenant == _tenant &&
                     x.UserKey == key.UserKey,
                ct);

        return projection?.ToDomain();
    }

    public async Task<bool> ExistsAsync(UserLifecycleKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await DbSet
            .AnyAsync(
                x => x.Tenant == _tenant &&
                     x.UserKey == key.UserKey,
                ct);
    }

    public async Task AddAsync(UserLifecycle entity, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (entity.Version != 0)
            throw new InvalidOperationException("New lifecycle must have version 0.");

        var projection = entity.ToProjection();

        DbSet.Add(projection);

        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(UserLifecycle entity, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var existing = await DbSet
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.UserKey == entity.UserKey,
                ct);

        if (existing is null)
            throw new UAuthNotFoundException("user_lifecycle_not_found");

        if (existing.Version != expectedVersion)
            throw new UAuthConcurrencyException("user_lifecycle_concurrency_conflict");

        entity.UpdateProjection(existing);
        existing.Version++;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(UserLifecycleKey key, long expectedVersion, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSet
            .SingleOrDefaultAsync(
                x => x.Tenant == _tenant &&
                     x.UserKey == key.UserKey,
                ct);

        if (projection is null)
            throw new UAuthNotFoundException("user_lifecycle_not_found");

        if (projection.Version != expectedVersion)
            throw new UAuthConcurrencyException("user_lifecycle_concurrency_conflict");

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

    public async Task<PagedResult<UserLifecycle>> QueryAsync(UserLifecycleQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();

        var baseQuery = DbSet
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant);

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(x => x.DeletedAt == null);

        if (query.Status != null)
            baseQuery = baseQuery.Where(x => x.Status == query.Status);

        baseQuery = query.SortBy switch
        {
            nameof(UserLifecycle.Id) =>
                query.Descending ? baseQuery.OrderByDescending(x => x.Id) : baseQuery.OrderBy(x => x.Id),

            nameof(UserLifecycle.CreatedAt) =>
                query.Descending ? baseQuery.OrderByDescending(x => x.CreatedAt) : baseQuery.OrderBy(x => x.CreatedAt),

            nameof(UserLifecycle.Status) =>
                query.Descending ? baseQuery.OrderByDescending(x => x.Status) : baseQuery.OrderBy(x => x.Status),

            _ => baseQuery.OrderBy(x => x.CreatedAt)
        };

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserLifecycle>(
            items.Select(x => x.ToDomain()).ToList(),
            total,
            normalized.PageNumber,
            normalized.PageSize,
            query.SortBy,
            query.Descending);
    }
}
