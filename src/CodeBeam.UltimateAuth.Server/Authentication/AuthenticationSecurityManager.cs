using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Security;

internal sealed class AuthenticationSecurityManager : IAuthenticationSecurityManager
{
    private readonly IAuthenticationSecurityStateStoreFactory _storeFactory;

    public AuthenticationSecurityManager(IAuthenticationSecurityStateStoreFactory storeFactory)
    {
        _storeFactory = storeFactory;
    }

    public async Task<AuthenticationSecurityState> GetOrCreateAccountAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var store = _storeFactory.Create(tenant);
        var state = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, credentialType: null, ct);

        if (state is not null)
            return state;

        var created = AuthenticationSecurityState.CreateAccount(tenant, userKey);
        await store.AddAsync(created, ct);
        return created;
    }

    public async Task<AuthenticationSecurityState> GetOrCreateFactorAsync(TenantKey tenant, UserKey userKey, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var store = _storeFactory.Create(tenant);
        var state = await store.GetAsync(userKey, AuthenticationSecurityScope.Factor, type, ct);

        if (state is not null)
            return state;

        var created = AuthenticationSecurityState.CreateFactor(tenant, userKey, type);
        await store.AddAsync(created, ct);
        return created;
    }

    public Task UpdateAsync(AuthenticationSecurityState updated, long expectedVersion, CancellationToken ct = default)
    {
        var store = _storeFactory.Create(updated.Tenant);
        return store.UpdateAsync(updated, expectedVersion, ct);
    }

    public Task DeleteAsync(TenantKey tenant, UserKey userKey, AuthenticationSecurityScope scope, CredentialType? credentialType, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var store = _storeFactory.Create(tenant);
        return store.DeleteAsync(userKey, scope, credentialType, ct);
    }
}
