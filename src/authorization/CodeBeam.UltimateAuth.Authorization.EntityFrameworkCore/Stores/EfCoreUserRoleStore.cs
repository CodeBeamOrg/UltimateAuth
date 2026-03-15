using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class EfCoreUserRoleStore : IUserRoleStore
{
    private readonly UAuthAuthorizationDbContext _db;

    public EfCoreUserRoleStore(UAuthAuthorizationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<UserRole>> GetAssignmentsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var entities = await _db.UserRoles
            .AsNoTracking()
            .Where(x =>
                x.Tenant == tenant &&
                x.UserKey == userKey)
            .ToListAsync(ct);

        return entities.Select(UserRoleMapper.ToDomain).ToList().AsReadOnly();
    }

    public async Task AssignAsync(TenantKey tenant, UserKey userKey, RoleId roleId, DateTimeOffset assignedAt, CancellationToken ct = default)
    {
        var exists = await _db.UserRoles
            .AnyAsync(x =>
                x.Tenant == tenant &&
                x.UserKey == userKey &&
                x.RoleId == roleId,
                ct);

        if (exists)
            throw new UAuthConflictException("role_already_assigned");

        var entity = new UserRoleProjection
        {
            Tenant = tenant,
            UserKey = userKey,
            RoleId = roleId,
            AssignedAt = assignedAt
        };

        _db.UserRoles.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(TenantKey tenant, UserKey userKey, RoleId roleId, CancellationToken ct = default)
    {
        var entity = await _db.UserRoles
            .SingleOrDefaultAsync(x =>
                x.Tenant == tenant &&
                x.UserKey == userKey &&
                x.RoleId == roleId,
                ct);

        if (entity is null)
            return;

        _db.UserRoles.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAssignmentsByRoleAsync(TenantKey tenant, RoleId roleId, CancellationToken ct = default)
    {
        var entities = await _db.UserRoles
            .Where(x =>
                x.Tenant == tenant &&
                x.RoleId == roleId)
            .ToListAsync(ct);

        if (entities.Count == 0)
            return;

        _db.UserRoles.RemoveRange(entities);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CountAssignmentsAsync(TenantKey tenant, RoleId roleId, CancellationToken ct = default)
    {
        return await _db.UserRoles
            .CountAsync(x =>
                x.Tenant == tenant &&
                x.RoleId == roleId,
                ct);
    }
}
