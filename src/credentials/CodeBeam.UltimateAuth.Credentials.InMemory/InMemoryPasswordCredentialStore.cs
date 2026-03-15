using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryPasswordCredentialStore : InMemoryVersionedStore<PasswordCredential, CredentialKey>, IPasswordCredentialStore
{
    protected override CredentialKey GetKey(PasswordCredential entity)
        => new(entity.Tenant, entity.Id);

    protected override void BeforeAdd(PasswordCredential entity)
    {
        var exists = Values()
            .Any(x =>
                x.Tenant == entity.Tenant &&
                x.UserKey == entity.UserKey &&
                !x.IsDeleted);

        if (exists)
            throw new UAuthConflictException("password_credential_exists");
    }

    public Task<IReadOnlyCollection<PasswordCredential>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = Values()
            .Where(x =>
                x.Tenant == tenant &&
                x.UserKey == userKey &&
                !x.IsDeleted)
            .Select(x => x.Snapshot())
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyCollection<PasswordCredential>>(result);
    }

    public Task RevokeAsync(CredentialKey key, DateTimeOffset revokedAt, long expectedVersion, CancellationToken ct = default)
    {
        if (!TryGet(key, out var credential) || credential is null)
            throw new UAuthNotFoundException("credential_not_found");

        var revoked = credential.Revoke(revokedAt);
        return SaveAsync(revoked, expectedVersion, ct);
    }

    public async Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        var credentials = Values()
            .Where(c => c.Tenant == tenant && c.UserKey == userKey)
            .ToList();

        foreach (var credential in credentials)
        {
            await DeleteAsync(new CredentialKey(tenant, credential.Id), credential.Version, mode, now, ct);
        }
    }
}
