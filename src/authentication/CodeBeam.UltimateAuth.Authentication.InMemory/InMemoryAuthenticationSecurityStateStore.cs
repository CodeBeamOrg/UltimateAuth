using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Authentication.InMemory;

internal sealed class InMemoryAuthenticationSecurityStateStore : IAuthenticationSecurityStateStore
{
    private readonly TenantKey _tenant;

    private readonly ConcurrentDictionary<Guid, AuthenticationSecurityState> _byId = new();
    private readonly ConcurrentDictionary<(UserKey, AuthenticationSecurityScope, CredentialType?), Guid> _index = new();

    public InMemoryAuthenticationSecurityStateStore(TenantExecutionContext tenant)
    {
        _tenant = tenant.Tenant;
    }

    public Task<AuthenticationSecurityState?> GetAsync(
        UserKey userKey,
        AuthenticationSecurityScope scope,
        CredentialType? credentialType,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = (userKey, scope, credentialType);

        if (_index.TryGetValue(key, out var id) &&
            _byId.TryGetValue(id, out var state))
        {
            return Task.FromResult<AuthenticationSecurityState?>(state.Snapshot());
        }

        return Task.FromResult<AuthenticationSecurityState?>(null);
    }

    public Task AddAsync(AuthenticationSecurityState state, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (state.Tenant != _tenant)
            throw new InvalidOperationException("Tenant mismatch.");

        var key = (state.UserKey, state.Scope, state.CredentialType);

        if (!_index.TryAdd(key, state.Id))
            throw new UAuthConflictException("security_state_already_exists");

        var snapshot = state.Snapshot();

        if (!_byId.TryAdd(state.Id, snapshot))
        {
            _index.TryRemove(key, out _);
            throw new UAuthConflictException("security_state_add_failed");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(AuthenticationSecurityState state, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (state.Tenant != _tenant)
            throw new InvalidOperationException("Tenant mismatch.");

        var key = (state.UserKey, state.Scope, state.CredentialType);

        if (!_index.TryGetValue(key, out var id) || id != state.Id)
            throw new UAuthConflictException("security_state_index_corrupted");

        if (!_byId.TryGetValue(state.Id, out var current))
            throw new UAuthNotFoundException("security_state_not_found");

        if (current.SecurityVersion != expectedVersion)
            throw new UAuthConflictException("security_state_version_conflict");

        var next = state.Snapshot();

        if (!_byId.TryUpdate(state.Id, next, current))
            throw new UAuthConflictException("security_state_update_conflict");

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        UserKey userKey,
        AuthenticationSecurityScope scope,
        CredentialType? credentialType,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = (userKey, scope, credentialType);

        if (!_index.TryRemove(key, out var id))
            return Task.CompletedTask;

        _byId.TryRemove(id, out _);

        return Task.CompletedTask;
    }
}
