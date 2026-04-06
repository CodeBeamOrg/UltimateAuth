using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class EfCoreUserRoleStore<TDbContext> : IUserRoleStore where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly TenantKey _tenant;

    public EfCoreUserRoleStore(TDbContext db, TenantExecutionContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    private DbSet<UserRoleProjection> DbSet => _db.Set<UserRoleProjection>();

    public async Task<IReadOnlyCollection<UserRole>> GetAssignmentsAsync(UserKey userKey, CancellationToken ct = default)
    {
        var entities = await DbSet
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey)
            .ToListAsync(ct);

        return entities.Select(UserRoleMapper.ToDomain).ToList().AsReadOnly();
    }

    public async Task AssignAsync(UserKey userKey, RoleId roleId, DateTimeOffset assignedAt, CancellationToken ct = default)
    {
        var exists = await DbSet
            .AnyAsync(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.RoleId == roleId,
                ct);

        if (exists)
            throw new UAuthConflictException("role_already_assigned");

        var entity = new UserRoleProjection
        {
            Tenant = _tenant,
            UserKey = userKey,
            RoleId = roleId,
            AssignedAt = assignedAt
        };

        DbSet.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(UserKey userKey, RoleId roleId, CancellationToken ct = default)
    {
        var entity = await DbSet
            .SingleOrDefaultAsync(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.RoleId == roleId,
                ct);

        if (entity is null)
            return;

        DbSet.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAssignmentsByRoleAsync(RoleId roleId, CancellationToken ct = default)
    {
        await DbSet
            .Where(x =>
                x.Tenant == _tenant &&
                x.RoleId == roleId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<int> CountAssignmentsAsync(RoleId roleId, CancellationToken ct = default)
    {
        return await DbSet
            .CountAsync(x =>
                x.Tenant == _tenant &&
                x.RoleId == roleId,
                ct);
    }
}
