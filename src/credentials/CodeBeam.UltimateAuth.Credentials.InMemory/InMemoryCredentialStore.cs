using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Reference;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryCredentialStore : ICredentialStore
{
    private readonly ConcurrentDictionary<(TenantKey, Guid), PasswordCredential> _store = new();

    public Task<IReadOnlyCollection<ICredential>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = _store.Values
            .Where(c => c.Tenant == tenant && c.UserKey == userKey)
            .Cast<ICredential>()
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ICredential>>(result);
    }

    public Task<ICredential?> GetByIdAsync(TenantKey tenant, Guid credentialId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _store.TryGetValue((tenant, credentialId), out var credential);
        return Task.FromResult<ICredential?>(credential);
    }

    public Task AddAsync(TenantKey tenant, ICredential credential, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // TODO: Support other credential types if needed. For now, we only have PasswordCredential in-memory.
        if (credential is not PasswordCredential pwd)
            throw new NotSupportedException("Only password credentials are supported in-memory.");

        var key = (tenant, pwd.Id);

        if (!_store.TryAdd(key, pwd))
            throw new UAuthConflictException("credential_already_exists");

        return Task.CompletedTask;
    }

    public Task UpdateAsync(TenantKey tenant, ICredential credential, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (credential is not PasswordCredential pwd)
            throw new NotSupportedException("Only password credentials are supported in-memory.");

        var key = (tenant, pwd.Id);

        if (!_store.ContainsKey(key))
            throw new UAuthNotFoundException("credential_not_found");

        if (pwd.Version != expectedVersion)
            throw new UAuthConflictException("credential_version_conflict");

        _store[key] = pwd;

        return Task.CompletedTask;
    }

    public Task RevokeAsync(TenantKey tenant, Guid credentialId, DateTimeOffset revokedAt, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = (tenant, credentialId);

        if (!_store.TryGetValue(key, out var credential))
            throw new UAuthNotFoundException("credential_not_found");

        if (credential.Version != expectedVersion)
            throw new UAuthConflictException("credential_version_conflict");

        if (credential.IsRevoked)
            return Task.CompletedTask;

        credential.Revoke(revokedAt);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TenantKey tenant, Guid credentialId, DeleteMode mode, DateTimeOffset now, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = (tenant, credentialId);

        if (!_store.TryGetValue(key, out var credential))
            throw new UAuthNotFoundException("credential_not_found");

        if (credential.Version != expectedVersion)
            throw new UAuthConflictException("credential_version_conflict");

        if (mode == DeleteMode.Hard)
        {
            _store.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        if (!credential.IsRevoked)
            credential.Revoke(now);

        return Task.CompletedTask;
    }

    public async Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var credentials = _store.Values.Where(c => c.Tenant == tenant && c.UserKey == userKey).ToList();

        foreach (var credential in credentials)
        {
            ct.ThrowIfCancellationRequested();

            await DeleteAsync(
                tenant,
                credential.Id,
                mode,
                now,
                credential.Version,
                ct);
        }
    }
}
