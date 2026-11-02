using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal sealed class EfCorePasswordHistoryStore<TDbContext>(TDbContext db, TenantKey tenant) : IPasswordHistoryStore where TDbContext : DbContext
{
    private DbSet<PasswordHistoryProjection> DbSet => db.Set<PasswordHistoryProjection>();

    public async Task<IReadOnlyList<PasswordHistoryEntry>> GetRecentAsync(CredentialKey credentialKey, int count, CancellationToken ct = default)
    {
        ValidateKey(credentialKey);

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var entries = await DbSet
            .AsNoTracking()
            .Where(x => x.Tenant == tenant && x.CredentialId == credentialKey.Id)
            .OrderByDescending(x => x.ReplacedAt)
            .ThenByDescending(x => x.Id)
            .Take(count)
            .ToListAsync(ct);

        return entries.Select(x => new PasswordHistoryEntry
        {
            Id = x.Id,
            Tenant = x.Tenant,
            UserKey = x.UserKey,
            CredentialKey = new CredentialKey(x.Tenant, x.CredentialId),
            SecretHash = x.SecretHash,
            ReplacedAt = x.ReplacedAt
        }).ToArray();
    }

    public async Task AddAsync(PasswordHistoryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateKey(entry.CredentialKey);

        if (entry.Tenant != tenant)
            throw new UAuthConflictException("tenant_mismatch");

        if (entry.Id == Guid.Empty)
            throw new ArgumentException("History entry ID is required.");

        DbSet.Add(new PasswordHistoryProjection
        {
            Id = entry.Id,
            Tenant = entry.Tenant,
            UserKey = entry.UserKey,
            CredentialId = entry.CredentialKey.Id,
            SecretHash = entry.SecretHash,
            ReplacedAt = entry.ReplacedAt
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task PruneAsync(CredentialKey credentialKey, int keepLast, CancellationToken ct = default)
    {
        ValidateKey(credentialKey);

        if (keepLast < 0)
            throw new ArgumentOutOfRangeException(nameof(keepLast));

        var keepIds = await DbSet
            .AsNoTracking()
            .Where(x => x.Tenant == tenant && x.CredentialId == credentialKey.Id)
            .OrderByDescending(x => x.ReplacedAt)
            .ThenByDescending(x => x.Id)
            .Take(keepLast)
            .Select(x => x.Id)
            .ToListAsync(ct);

        await DbSet
            .Where(x =>
                x.Tenant == tenant &&
                x.CredentialId == credentialKey.Id &&
                !keepIds.Contains(x.Id))
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteByCredentialAsync(CredentialKey credentialKey, CancellationToken ct = default)
    {
        ValidateKey(credentialKey);

        await DbSet
            .Where(x =>
                x.Tenant == tenant &&
                x.CredentialId == credentialKey.Id)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        await DbSet
            .Where(x =>
                x.Tenant == tenant &&
                x.UserKey == userKey)
            .ExecuteDeleteAsync(ct);
    }

    private void ValidateKey(CredentialKey key)
    {
        if (key.Tenant != tenant)
            throw new UAuthConflictException("tenant_mismatch");
    }
}
