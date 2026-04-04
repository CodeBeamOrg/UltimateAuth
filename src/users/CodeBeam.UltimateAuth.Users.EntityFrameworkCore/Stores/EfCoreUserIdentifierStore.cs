using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class EfCoreUserIdentifierStore<TDbContext> : IUserIdentifierStore where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly TenantKey _tenant;

    public EfCoreUserIdentifierStore(TDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    private DbSet<UserIdentifierProjection> DbSet => _db.Set<UserIdentifierProjection>();

    public async Task<bool> ExistsAsync(Guid key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await DbSet
            .AnyAsync(x =>
                x.Id == key &&
                x.Tenant == _tenant,
                ct);
    }

    public async Task<IdentifierExistenceResult> ExistsAsync(IdentifierExistenceQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var q = DbSet
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.Type == query.Type &&
                x.NormalizedValue == query.NormalizedValue &&
                x.DeletedAt == null);

        if (query.ExcludeIdentifierId.HasValue)
            q = q.Where(x => x.Id != query.ExcludeIdentifierId.Value);

        q = query.Scope switch
        {
            IdentifierExistenceScope.WithinUser =>
                q.Where(x => x.UserKey == query.UserKey),

            IdentifierExistenceScope.TenantPrimaryOnly =>
                q.Where(x => x.IsPrimary),

            IdentifierExistenceScope.TenantAny =>
                q,

            _ => q
        };

        var match = await q
            .Select(x => new
            {
                x.Id,
                x.UserKey,
                x.IsPrimary
            })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return new IdentifierExistenceResult(false);

        return new IdentifierExistenceResult(
            true,
            match.UserKey,
            match.Id,
            match.IsPrimary);
    }

    public async Task<UserIdentifier?> GetAsync(Guid key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == key &&
                x.Tenant == _tenant,
                ct);

        return projection?.ToDomain();
    }

    public async Task<UserIdentifier?> GetAsync(UserIdentifierType type, string normalizedValue, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.Tenant == _tenant &&
                    x.Type == type &&
                    x.NormalizedValue == normalizedValue &&
                    x.DeletedAt == null,
                ct);

        return projection?.ToDomain();
    }

    public async Task<UserIdentifier?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == id &&
                x.Tenant == _tenant,
                ct);

        return projection?.ToDomain();
    }

    public async Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projections = await DbSet
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.DeletedAt == null)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return projections.Select(x => x.ToDomain()).ToList();
    }

    public async Task AddAsync(UserIdentifier entity, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (entity.Version != 0)
            throw new UAuthValidationException("New identifier must have version 0.");

        var projection = entity.ToProjection();

        using var tx = await _db.Database.BeginTransactionAsync(ct);

        if (entity.IsPrimary)
        {
            await DbSet
                .Where(x =>
                    x.Tenant == _tenant &&
                    x.UserKey == entity.UserKey &&
                    x.Type == entity.Type &&
                    x.IsPrimary &&
                    x.DeletedAt == null)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(i => i.IsPrimary, false),
                    ct);
        }

        DbSet.Add(projection);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task SaveAsync(UserIdentifier entity, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var tx = await _db.Database.BeginTransactionAsync(ct);

        if (entity.IsPrimary)
        {
            await DbSet
                .Where(x =>
                    x.Tenant == _tenant &&
                    x.UserKey == entity.UserKey &&
                    x.Type == entity.Type &&
                    x.Id != entity.Id &&
                    x.IsPrimary &&
                    x.DeletedAt == null)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(i => i.IsPrimary, false),
                    ct);
        }

        var existing = await DbSet
            .SingleOrDefaultAsync(x =>
                x.Id == entity.Id &&
                x.Tenant == _tenant,
                ct);

        if (existing is null)
            throw new UAuthNotFoundException("identifier_not_found");

        if (existing.Version != expectedVersion)
            throw new UAuthConcurrencyException("identifier_concurrency_conflict");

        entity.UpdateProjection(existing);

        existing.Version++;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task DeleteAsync(Guid key, long expectedVersion, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var projection = await DbSet
            .SingleOrDefaultAsync(x =>
                x.Id == key &&
                x.Tenant == _tenant,
                ct);

        if (projection is null)
            throw new UAuthNotFoundException("identifier_not_found");

        if (projection.Version != expectedVersion)
            throw new UAuthConcurrencyException("identifier_concurrency_conflict");

        if (mode == DeleteMode.Hard)
        {
            DbSet.Remove(projection);
        }
        else
        {
            projection.DeletedAt = now;
            projection.IsPrimary = false;
            projection.Version++;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteByUserAsync(UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (mode == DeleteMode.Hard)
        {
            await DbSet
                .Where(x =>
                    x.Tenant == _tenant &&
                    x.UserKey == userKey)
                .ExecuteDeleteAsync(ct);

            return;
        }

        await DbSet
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.DeletedAt == null)
            .ExecuteUpdateAsync(
                x => x
                    .SetProperty(i => i.DeletedAt, deletedAt)
                    .SetProperty(i => i.IsPrimary, false),
                ct);
    }

    public async Task<IReadOnlyList<UserIdentifier>> GetByUsersAsync(IReadOnlyList<UserKey> userKeys, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (userKeys is null || userKeys.Count == 0)
            return Array.Empty<UserIdentifier>();

        var projections = await DbSet
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                userKeys.Contains(x.UserKey) &&
                x.DeletedAt == null)
            .OrderBy(x => x.UserKey)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return projections
            .Select(x => x.ToDomain())
            .ToList();
    }

    public async Task<PagedResult<UserIdentifier>> QueryAsync(UserIdentifierQuery query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (query.UserKey is null)
            throw new UAuthIdentifierValidationException("userKey_required");

        var normalized = query.Normalize();

        var baseQuery = DbSet
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == query.UserKey);

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(x => x.DeletedAt == null);

        baseQuery = query.SortBy switch
        {
            nameof(UserIdentifier.Type) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.Type)
                    : baseQuery.OrderBy(x => x.Type),

            nameof(UserIdentifier.CreatedAt) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.CreatedAt)
                    : baseQuery.OrderBy(x => x.CreatedAt),

            nameof(UserIdentifier.Value) =>
                query.Descending
                    ? baseQuery.OrderByDescending(x => x.Value)
                    : baseQuery.OrderBy(x => x.Value),

            _ => baseQuery.OrderBy(x => x.CreatedAt)
        };

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserIdentifier>(
            items.Select(x => x.ToDomain()).ToList(),
            total,
            normalized.PageNumber,
            normalized.PageSize,
            query.SortBy,
            query.Descending);
    }
}
