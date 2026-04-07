using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class EfCoreRoleStore<TDbContext> : IRoleStore where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly TenantKey _tenant;

    public EfCoreRoleStore(TDbContext db, TenantExecutionContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    private DbSet<RoleProjection> DbSetRole => _db.Set<RoleProjection>();
    private DbSet<RolePermissionProjection> DbSetPermission => _db.Set<RolePermissionProjection>();

    public async Task<bool> ExistsAsync(RoleKey key, CancellationToken ct = default)
    {
        return await DbSetRole
            .AnyAsync(x =>
                x.Tenant == _tenant &&
                x.Id == key.RoleId,
                ct);
    }

    public async Task AddAsync(Role role, CancellationToken ct = default)
    {
        var exists = await DbSetRole
            .AnyAsync(x =>
                x.Tenant == _tenant &&
                x.NormalizedName == role.NormalizedName &&
                x.DeletedAt == null,
                ct);

        if (exists)
            throw new UAuthConflictException("role_already_exists");

        var entity = RoleMapper.ToProjection(role);

        DbSetRole.Add(entity);

        var permissionEntities = role.Permissions
            .Select(p => RolePermissionMapper.ToProjection(_tenant, role.Id, p));

        DbSetPermission.AddRange(permissionEntities);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<Role?> GetAsync(RoleKey key, CancellationToken ct = default)
    {
        var entity = await DbSetRole
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.Id == key.RoleId,
                ct);

        if (entity is null)
            return null;

        var permissions = await DbSetPermission
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.RoleId == key.RoleId)
            .ToListAsync(ct);

        return RoleMapper.ToDomain(entity, permissions);
    }

    public async Task SaveAsync(Role role, long expectedVersion, CancellationToken ct = default)
    {
        var entity = await DbSetRole
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.Id == role.Id,
                ct);

        if (entity is null)
            throw new UAuthNotFoundException("role_not_found");

        if (entity.Version != expectedVersion)
            throw new UAuthConcurrencyException("role_version_conflict");

        if (entity.NormalizedName != role.NormalizedName)
        {
            var exists = await DbSetRole
                .AnyAsync(x =>
                    x.Tenant == _tenant &&
                    x.NormalizedName == role.NormalizedName &&
                    x.Id != role.Id &&
                    x.DeletedAt == null,
                    ct);

            if (exists)
                throw new UAuthConflictException("role_name_already_exists");
        }

        RoleMapper.UpdateProjection(role, entity);
        entity.Version++;

        var existingPermissions = await DbSetPermission
            .Where(x =>
                x.Tenant == _tenant &&
                x.RoleId == role.Id)
            .ToListAsync(ct);

        DbSetPermission.RemoveRange(existingPermissions);

        var newPermissions = role.Permissions
            .Select(p => RolePermissionMapper.ToProjection(_tenant, role.Id, p));

        DbSetPermission.AddRange(newPermissions);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(RoleKey key, long expectedVersion, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        var entity = await DbSetRole
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.Id == key.RoleId,
                ct);

        if (entity is null)
            throw new UAuthNotFoundException("role_not_found");

        if (entity.Version != expectedVersion)
            throw new UAuthConcurrencyException("role_version_conflict");

        if (mode == DeleteMode.Hard)
        {
            await DbSetPermission
                .Where(x =>
                    x.Tenant == _tenant &&
                    x.RoleId == key.RoleId)
                .ExecuteDeleteAsync(ct);

            DbSetRole.Remove(entity);
        }
        else
        {
            entity.DeletedAt = now;
            entity.Version++;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<Role?> GetByNameAsync(string normalizedName, CancellationToken ct = default)
    {
        var entity = await DbSetRole
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.NormalizedName == normalizedName &&
                x.DeletedAt == null,
                ct);

        if (entity is null)
            return null;

        var permissions = await DbSetPermission
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.RoleId == entity.Id)
            .ToListAsync(ct);

        return RoleMapper.ToDomain(entity, permissions);
    }

    public async Task<IReadOnlyCollection<Role>> GetByIdsAsync(
        IReadOnlyCollection<RoleId> roleIds,
        CancellationToken ct = default)
    {
        var entities = await DbSetRole
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                roleIds.Contains(x.Id))
            .ToListAsync(ct);

        var roleIdsSet = entities.Select(x => x.Id).ToList();

        var permissions = await DbSetPermission
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                roleIdsSet.Contains(x.RoleId))
            .ToListAsync(ct);

        var permissionLookup = permissions
            .GroupBy(x => x.RoleId)
            .ToDictionary(x => x.Key);

        var result = new List<Role>(entities.Count);

        foreach (var entity in entities)
        {
            permissionLookup.TryGetValue(entity.Id, out var perms);

            result.Add(RoleMapper.ToDomain(entity, perms ?? Enumerable.Empty<RolePermissionProjection>()));
        }

        return result.AsReadOnly();
    }

    public async Task<PagedResult<Role>> QueryAsync(
        RoleQuery query,
        CancellationToken ct = default)
    {
        var normalized = query.Normalize();

        var baseQuery = DbSetRole
            .AsNoTracking()
            .Where(x => x.Tenant == _tenant);

        if (!query.IncludeDeleted)
            baseQuery = baseQuery.Where(x => x.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            baseQuery = baseQuery.Where(x => x.NormalizedName.Contains(search));
        }

        baseQuery = query.SortBy switch
        {
            nameof(Role.CreatedAt) =>
                query.Descending ? baseQuery.OrderByDescending(x => x.CreatedAt) : baseQuery.OrderBy(x => x.CreatedAt),

            nameof(Role.UpdatedAt) =>
                query.Descending ? baseQuery.OrderByDescending(x => x.UpdatedAt) : baseQuery.OrderBy(x => x.UpdatedAt),

            nameof(Role.Name) =>
                query.Descending ? baseQuery.OrderByDescending(x => x.Name) : baseQuery.OrderBy(x => x.Name),

            nameof(Role.NormalizedName) =>
                query.Descending ? baseQuery.OrderByDescending(x => x.NormalizedName) : baseQuery.OrderBy(x => x.NormalizedName),

            _ => baseQuery.OrderBy(x => x.CreatedAt)
        };

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(ct);

        var result = items
            .Select(x => RoleMapper.ToDomain(x, Enumerable.Empty<RolePermissionProjection>()))
            .ToList()
            .AsReadOnly();

        return new PagedResult<Role>(
            result,
            total,
            normalized.PageNumber,
            normalized.PageSize,
            query.SortBy,
            query.Descending);
    }
}
