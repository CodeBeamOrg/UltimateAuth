using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal sealed class EfCorePasswordCredentialStore<TDbContext> : IPasswordCredentialStore where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly TenantKey _tenant;

    public EfCorePasswordCredentialStore(TDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    private DbSet<PasswordCredentialProjection> DbSet => _db.Set<PasswordCredentialProjection>();

    public async Task<bool> ExistsAsync(CredentialKey key, CancellationToken ct = default)
    {
        return await DbSet
            .AnyAsync(x =>
                x.Id == key.Id &&
                x.Tenant == _tenant,
                ct);
    }

    public async Task AddAsync(PasswordCredential credential, CancellationToken ct = default)
    {
        var entity = credential.ToProjection();

        DbSet.Add(entity);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<PasswordCredential?> GetAsync(CredentialKey key, CancellationToken ct = default)
    {
        var entity = await DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == key.Id &&
                     x.Tenant == _tenant,
                ct);

        return entity?.ToDomain();
    }

    public async Task SaveAsync(PasswordCredential credential, long expectedVersion, CancellationToken ct = default)
    {
        var entity = await DbSet
            .SingleOrDefaultAsync(x =>
                x.Id == credential.Id &&
                x.Tenant == _tenant,
                ct);

        if (entity is null)
            throw new UAuthNotFoundException("credential_not_found");

        if (entity.Version != expectedVersion)
            throw new UAuthConcurrencyException("credential_version_conflict");

        credential.UpdateProjection(entity);
        entity.Version++;

        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(CredentialKey key, DateTimeOffset revokedAt, long expectedVersion, CancellationToken ct = default)
    {
        var entity = await DbSet
            .SingleOrDefaultAsync(x =>
                x.Id == key.Id &&
                x.Tenant == _tenant,
                ct);

        if (entity is null)
            throw new UAuthNotFoundException("credential_not_found");

        if (entity.Version != expectedVersion)
            throw new UAuthConcurrencyException("credential_version_conflict");

        var domain = entity.ToDomain().Revoke(revokedAt);
        domain.UpdateProjection(entity);

        entity.Version++;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(CredentialKey key, long expectedVersion, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        var entity = await DbSet
            .SingleOrDefaultAsync(x =>
                x.Id == key.Id &&
                x.Tenant == _tenant,
                ct);

        if (entity is null)
            throw new UAuthNotFoundException("credential_not_found");

        if (entity.Version != expectedVersion)
            throw new UAuthConcurrencyException("credential_version_conflict");

        if (mode == DeleteMode.Hard)
        {
            DbSet.Remove(entity);
        }
        else
        {
            var domain = entity.ToDomain().MarkDeleted(now);
            domain.UpdateProjection(entity);
            entity.Version++;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<PasswordCredential>> GetByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        var entities = await DbSet
            .AsNoTracking()
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.DeletedAt == null)
            .ToListAsync(ct);

        return entities
            .Select(x => x.ToDomain())
            .ToList()
            .AsReadOnly();
    }

    public async Task DeleteByUserAsync(UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
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
            .ExecuteUpdateAsync(x =>
                x
                    .SetProperty(c => c.DeletedAt, now)
                    .SetProperty(c => c.Version, c => c.Version + 1),
                ct);
    }
}
