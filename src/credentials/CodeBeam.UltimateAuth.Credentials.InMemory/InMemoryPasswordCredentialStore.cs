using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.InMemory;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryPasswordCredentialStore : InMemoryTenantVersionedStore<PasswordCredential, CredentialKey>, IPasswordCredentialStore
{
    protected override CredentialKey GetKey(PasswordCredential entity)
        => new(entity.Tenant, entity.Id);

    public InMemoryPasswordCredentialStore(TenantContext tenant) : base(tenant)
    {
    }

    protected override void BeforeAdd(PasswordCredential entity)
    {
        var exists = TenantValues()
            .Any(x =>
                x.Tenant == entity.Tenant &&
                x.UserKey == entity.UserKey &&
                !x.IsDeleted);

        if (exists)
            throw new UAuthConflictException("password_credential_exists");
    }

    public Task<IReadOnlyCollection<PasswordCredential>> GetByUserAsync(UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = TenantValues()
            .Where(x =>
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

    public async Task DeleteByUserAsync(UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        var credentials = TenantValues()
            .Where(c => c.UserKey == userKey)
            .ToList();

        foreach (var credential in credentials)
        {
            await DeleteAsync(GetKey(credential), credential.Version, mode, now, ct);
        }
    }
}
