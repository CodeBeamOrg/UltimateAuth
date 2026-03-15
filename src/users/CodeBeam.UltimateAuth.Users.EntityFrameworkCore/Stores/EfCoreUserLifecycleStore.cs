using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserLifecycleStore : IUserLifecycleStore
{
    private readonly UAuthUserDbContext _db;

    public EfCoreUserLifecycleStore(UAuthUserDbContext db)
    {
        _db = db;
    }

    public async Task<UserLifecycle?> GetAsync(UserLifecycleKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Lifecycles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Tenant == key.Tenant &&
                     x.UserKey == key.UserKey,
                ct);

        return projection?.ToDomain();
    }

    public async Task<bool> ExistsAsync(UserLifecycleKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await _db.Lifecycles
            .AnyAsync(
                x => x.Tenant == key.Tenant &&
                     x.UserKey == key.UserKey,
                ct);
    }

    public async Task AddAsync(UserLifecycle entity, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = entity.ToProjection();

        if (entity.Version != 0)
            throw new InvalidOperationException("New lifecycle must have version 0.");

        _db.Lifecycles.Add(projection);

        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(UserLifecycle entity, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = entity.ToProjection();

        _db.Entry(projection).State = EntityState.Modified;

        _db.Entry(projection)
            .Property(x => x.Version)
            .OriginalValue = expectedVersion;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new UAuthConcurrencyException("user_lifecycle_concurrency_conflict");
        }
    }

    public async Task DeleteAsync(UserLifecycleKey key, long expectedVersion, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await _db.Lifecycles
            .SingleOrDefaultAsync(
                x => x.Tenant == key.Tenant &&
                     x.UserKey == key.UserKey,
                ct);

        if (projection is null)
            throw new UAuthNotFoundException("user_lifecycle_not_found");

        if (projection.Version != expectedVersion)
            throw new UAuthConcurrencyException("user_lifecycle_concurrency_conflict");

        if (mode == DeleteMode.Hard)
        {
            _db.Lifecycles.Remove(projection);
        }
        else
        {
            projection.DeletedAt = now;
            projection.Version++;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<UserLifecycle>> QueryAsync(TenantKey tenant, UserLifecycleQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = query.Normalize();

        var baseQuery = _db.Lifecycles
            .AsNoTracking()
            .Where(x => x.Tenant == tenant);

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(x => x.DeletedAt == null);

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
