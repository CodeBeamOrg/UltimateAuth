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
    private readonly TenantContext _tenant;

    public EfCorePasswordCredentialStore(UAuthCredentialDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<bool> ExistsAsync(CredentialKey key, CancellationToken ct = default)
    {
        return await _db.PasswordCredentials
            .AnyAsync(x =>
                x.Id == key.Id &&
                x.Tenant == key.Tenant,
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
                     x.Tenant == key.Tenant,
                ct);

        return entity?.ToDomain();
    }

    public async Task SaveAsync(PasswordCredential credential, long expectedVersion, CancellationToken ct = default)
    {
        var entity = await _db.PasswordCredentials
            .SingleOrDefaultAsync(x =>
                x.Id == credential.Id &&
                x.Tenant == credential.Tenant,
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
        var credential = await GetAsync(key, ct);

        if (credential is null)
            throw new UAuthNotFoundException("credential_not_found");

        var revoked = credential.Revoke(revokedAt);
        await SaveAsync(revoked, expectedVersion, ct);
    }

    public async Task DeleteAsync(CredentialKey key, long expectedVersion, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        var entity = await _db.PasswordCredentials
            .SingleOrDefaultAsync(x =>
                x.Id == key.Id &&
                x.Tenant == key.Tenant,
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
            entity.DeletedAt = now;
            entity.Version++;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<PasswordCredential>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var entities = await _db.PasswordCredentials
            .AsNoTracking()
            .Where(x =>
                x.Tenant == tenant &&
                x.UserKey == userKey &&
                x.DeletedAt == null)
            .ToListAsync(ct);

        return entities
            .Select(x => x.ToDomain())
            .ToList()
            .AsReadOnly();
    }

    public async Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        var entities = await _db.PasswordCredentials
            .Where(x =>
                x.Tenant == tenant &&
                x.UserKey == userKey)
            .ToListAsync(ct);

        foreach (var entity in entities)
        {
            if (mode == DeleteMode.Hard)
            {
                _db.PasswordCredentials.Remove(entity);
            }
            else
            {
                entity.DeletedAt = now;
                entity.Version++;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}