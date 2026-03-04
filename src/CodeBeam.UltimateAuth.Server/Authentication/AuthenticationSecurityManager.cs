using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Security;

internal sealed class AuthenticationSecurityManager : IAuthenticationSecurityManager
{
    private readonly IAuthenticationSecurityStateStore _store;
    private readonly UAuthServerOptions _options;

    public AuthenticationSecurityManager(IAuthenticationSecurityStateStore store, IOptions<UAuthServerOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public async Task<AuthenticationSecurityState> GetOrCreateAccountAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var state = await _store.GetAsync(tenant, userKey, AuthenticationSecurityScope.Account, credentialType: null, ct);

        if (state is not null)
            return state;

        var created = AuthenticationSecurityState.CreateAccount(tenant, userKey);
        await _store.AddAsync(created, ct);
        return created;
    }

    public async Task<AuthenticationSecurityState> GetOrCreateFactorAsync(TenantKey tenant, UserKey userKey, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var state = await _store.GetAsync(tenant, userKey, AuthenticationSecurityScope.Factor, type, ct);

        if (state is not null)
            return state;

        var created = AuthenticationSecurityState.CreateFactor(tenant, userKey, type);
        await _store.AddAsync(created, ct);
        return created;
    }

    public Task UpdateAsync(AuthenticationSecurityState updated, long expectedVersion, CancellationToken ct = default)
    {
        return _store.UpdateAsync(updated, expectedVersion, ct);
    }

    public Task DeleteAsync(TenantKey tenant, UserKey userKey, AuthenticationSecurityScope scope, CredentialType? credentialType, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return _store.DeleteAsync(tenant, userKey, scope, credentialType, ct);
    }
}
