using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryCredentialStore : InMemoryVersionedStore<PasswordCredential, CredentialKey>, ICredentialStore
{
    protected override CredentialKey GetKey(PasswordCredential entity) => new(entity.Tenant, entity.Id);

    public Task<IReadOnlyCollection<ICredential>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = Values()
            .Where(c => c.Tenant == tenant && c.UserKey == userKey)
            .Cast<ICredential>()
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ICredential>>(result);
    }

    public Task<ICredential?> GetByIdAsync(CredentialKey key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (TryGet(key, out var entity))
            return Task.FromResult<ICredential?>(entity);

        return Task.FromResult<ICredential?>(entity);
    }

    public Task AddAsync(ICredential credential, CancellationToken ct = default)
    {
        // TODO: Implement other credential types
        if (credential is not PasswordCredential pwd)
            throw new NotSupportedException("Only password credentials are supported in-memory.");

        return base.AddAsync(pwd, ct);
    }

    public Task SaveAsync(ICredential credential, long expectedVersion, CancellationToken ct = default)
    {
        if (credential is not PasswordCredential pwd)
            throw new NotSupportedException("Only password credentials are supported in-memory.");

        return base.SaveAsync(pwd, expectedVersion, ct);
    }

    public Task RevokeAsync(CredentialKey key, DateTimeOffset revokedAt, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!TryGet(key, out var credential))
            throw new UAuthNotFoundException("credential_not_found");

        if (credential is not PasswordCredential pwd)
            throw new NotSupportedException("Only password credentials are supported in-memory.");

        var revoked = pwd.Revoke(revokedAt);

        return SaveAsync(revoked, expectedVersion, ct);
    }

    public Task DeleteAsync(CredentialKey key, DeleteMode mode, DateTimeOffset now, long expectedVersion, CancellationToken ct = default)
    {
        return base.DeleteAsync(key, expectedVersion, mode, now, ct);
    }

    public async Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var credentials = Values()
            .Where(c => c.Tenant == tenant && c.UserKey == userKey)
            .ToList();

        foreach (var credential in credentials)
        {
            await DeleteAsync(new CredentialKey(tenant, credential.Id), mode, now, credential.Version, ct);
        }
    }
}
