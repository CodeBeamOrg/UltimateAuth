using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal sealed class EfCorePasswordCredentialStore : IPasswordCredentialStore
{
    private readonly UAuthCredentialDbContext _db;
    private readonly TenantKey _tenant;

    public EfCorePasswordCredentialStore(UAuthCredentialDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    public async Task<bool> ExistsAsync(CredentialKey key, CancellationToken ct = default)
    {
        return await _db.PasswordCredentials
            .AnyAsync(x =>
                x.Id == key.Id &&
                x.Tenant == _tenant,
                ct);
    }

    public async Task AddAsync(PasswordCredential credential, CancellationToken ct = default)
    {
        var entity = credential.ToProjection();

        _db.PasswordCredentials.Add(entity);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<PasswordCredential?> GetAsync(CredentialKey key, CancellationToken ct = default)
    {
        var entity = await _db.PasswordCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == key.Id &&
                     x.Tenant == _tenant,
                ct);

        return entity?.ToDomain();
    }

    public async Task SaveAsync(PasswordCredential credential, long expectedVersion, CancellationToken ct = default)
    {
        var entity = await _db.PasswordCredentials
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
        var entity = await _db.PasswordCredentials
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
        var entity = await _db.PasswordCredentials
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
            _db.PasswordCredentials.Remove(entity);
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
        var entities = await _db.PasswordCredentials
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
            await _db.PasswordCredentials
                .Where(x =>
                    x.Tenant == _tenant &&
                    x.UserKey == userKey)
                .ExecuteDeleteAsync(ct);

            return;
        }

        await _db.PasswordCredentials
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
