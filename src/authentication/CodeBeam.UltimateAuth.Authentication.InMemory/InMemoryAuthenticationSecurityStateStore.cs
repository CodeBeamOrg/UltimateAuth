using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Authentication.InMemory;

internal sealed class InMemoryAuthenticationSecurityStateStore : IAuthenticationSecurityStateStore
{
    private readonly ConcurrentDictionary<Guid, AuthenticationSecurityState> _byId = new();
    private readonly ConcurrentDictionary<(TenantKey, UserKey, AuthenticationSecurityScope, CredentialType?), Guid> _index = new();

    public Task<AuthenticationSecurityState?> GetAsync(TenantKey tenant, UserKey userKey, AuthenticationSecurityScope scope, CredentialType? credentialType, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_index.TryGetValue((tenant, userKey, scope, credentialType), out var id) && _byId.TryGetValue(id, out var state))
        {
            return Task.FromResult<AuthenticationSecurityState?>(state);
        }

        return Task.FromResult<AuthenticationSecurityState?>(null);
    }

    public Task AddAsync(AuthenticationSecurityState state, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = (state.Tenant, state.UserKey, state.Scope, state.CredentialType);

        if (!_index.TryAdd(key, state.Id))
            throw new InvalidOperationException("security_state_already_exists");

        if (!_byId.TryAdd(state.Id, state))
        {
            _index.TryRemove(key, out _);
            throw new InvalidOperationException("security_state_add_failed");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(AuthenticationSecurityState state, long expectedVersion, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_byId.TryGetValue(state.Id, out var current))
            throw new InvalidOperationException("security_state_not_found");

        if (current.SecurityVersion != expectedVersion)
            throw new InvalidOperationException("security_state_version_conflict");

        _byId[state.Id] = state;

        return Task.CompletedTask;
    }
}
