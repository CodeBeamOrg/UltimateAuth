using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

internal sealed class InMemoryCredentialStore : ICredentialStore
{
    private readonly ConcurrentDictionary<(TenantKey Tenant, Guid Id), PasswordCredential> _byId = new();
    private readonly ConcurrentDictionary<(TenantKey Tenant, UserKey UserKey), ConcurrentDictionary<Guid, byte>> _byUser = new();

    private readonly IUAuthPasswordHasher _hasher;

    public InMemoryCredentialStore(IUAuthPasswordHasher hasher)
    {
        _hasher = hasher;
    }

    public Task<IReadOnlyCollection<ICredential>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byUser.TryGetValue((tenant, userKey), out var ids) || ids.Count == 0)
            return Task.FromResult<IReadOnlyCollection<ICredential>>(Array.Empty<ICredential>());

        var list = new List<ICredential>(ids.Count);

        foreach (var id in ids.Keys)
        {
            if (_byId.TryGetValue((tenant, id), out var cred))
            {
                list.Add(cred);
            }
        }

        return Task.FromResult<IReadOnlyCollection<ICredential>>(list);
    }

    public Task<ICredential?> GetByIdAsync(TenantKey tenant, Guid credentialId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _byId.TryGetValue((tenant, credentialId), out var cred);
        return Task.FromResult<ICredential?>(cred);
    }

    public Task<bool> ExistsAsync(TenantKey tenant, UserKey userKey, CredentialType type, string? secretHash, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byUser.TryGetValue((tenant, userKey), out var ids) || ids.Count == 0)
            return Task.FromResult(false);

        foreach (var id in ids.Keys)
        {
            if (!_byId.TryGetValue((tenant, id), out var cred))
                continue;

            if (cred.Type != type)
                continue;

            if (secretHash is null)
                return Task.FromResult(true);

            if (string.Equals(cred.SecretHash, secretHash, StringComparison.Ordinal))
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task AddAsync(TenantKey tenant, ICredential credential, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // TODO: Support other credential types if needed. For now, we only have PasswordCredential in-memory.
        if (credential is not PasswordCredential pwd)
            throw new NotSupportedException("Only password credentials are supported in-memory.");

        var id = pwd.Id == Guid.Empty ? Guid.NewGuid() : pwd.Id;

        var key = (tenant, id);
        if (_byId.ContainsKey(key))
            throw new InvalidOperationException("credential_already_exists");

        if (pwd.Id == Guid.Empty)
            throw new InvalidOperationException("credential_id_required");

        if (!_byId.TryAdd(key, pwd))
            throw new InvalidOperationException("credential_already_exists");

        var userIndex = _byUser.GetOrAdd((tenant, pwd.UserKey), _ => new ConcurrentDictionary<Guid, byte>());
        userIndex.TryAdd(pwd.Id, 0);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(TenantKey tenant, ICredential credential, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (credential is not PasswordCredential pwd)
            throw new NotSupportedException("Only password credentials are supported in-memory.");

        var key = (tenant, pwd.Id);

        if (!_byId.ContainsKey(key))
            throw new InvalidOperationException("credential_not_found");

        _byId[key] = pwd;

        return Task.CompletedTask;
    }

    public Task RevokeAsync(TenantKey tenant, Guid credentialId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = (tenant, credentialId);

        if (!_byId.TryGetValue(key, out var cred))
            throw new InvalidOperationException("credential_not_found");

        if (cred.IsRevoked)
            return Task.CompletedTask;

        cred.Revoke(revokedAt);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TenantKey tenant, Guid credentialId, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = (tenant, credentialId);

        if (!_byId.TryGetValue(key, out var cred))
            return Task.CompletedTask;

        if (mode == DeleteMode.Hard)
        {
            _byId.TryRemove(key, out _);

            if (_byUser.TryGetValue((tenant, cred.UserKey), out var set))
            {
                set.TryRemove(credentialId, out _);
            }

            return Task.CompletedTask;
        }

        if (!cred.IsRevoked)
            cred.Revoke(now);

        return Task.CompletedTask;
    }

    public Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byUser.TryGetValue((tenant, userKey), out var ids))
            return Task.CompletedTask;

        foreach (var id in ids.Keys.ToList())
        {
            DeleteAsync(tenant, id, mode, now, ct);
        }

        return Task.CompletedTask;
    }
}
