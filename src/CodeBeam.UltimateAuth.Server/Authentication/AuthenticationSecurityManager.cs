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

        try
        {
            await store.AddAsync(created, ct);
            return created;
        }
        catch (UAuthConflictException)
        {
            var existing = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, credentialType: null, ct);

            if (existing is not null)
                return existing;

            throw;
        }
    }

    public async Task<AuthenticationSecurityState> GetOrCreateFactorAsync(TenantKey tenant, UserKey userKey, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var store = _storeFactory.Create(tenant);
        var state = await store.GetAsync(userKey, AuthenticationSecurityScope.Factor, type, ct);

        if (state is not null)
            return state;

        var created = AuthenticationSecurityState.CreateFactor(tenant, userKey, type);

        try
        {
            await store.AddAsync(created, ct);
            return created;
        }
        catch (UAuthConflictException)
        {
            var existing = await store.GetAsync(userKey, AuthenticationSecurityScope.Factor, type, ct);

            if (existing is not null)
                return existing;

            throw;
        }
    }

    public async Task<AuthenticationSecurityState> MutateFactorAsync(TenantKey tenant, UserKey userKey, CredentialType type, Func<AuthenticationSecurityState, AuthenticationSecurityState> mutation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        const int maxAttempts = 5;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var current = await GetOrCreateFactorAsync(tenant, userKey, type, ct);

            var updated = mutation(current);

            if (ReferenceEquals(updated, current))
                return current;

            try
            {
                await UpdateAsync(updated, current.SecurityVersion, ct);

                return updated;
            }
            catch (UAuthConflictException) when (attempt < maxAttempts - 1)
            {
                // State changed after it was read.
                // Reload and reapply the domain mutation.
            }
        }

        throw new InvalidOperationException("Unreachable.");
    }

    public async Task<AuthenticationSecurityState> MutateAccountAsync(TenantKey tenant, UserKey userKey, Func<AuthenticationSecurityState, AuthenticationSecurityState> mutation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        const int maxAttempts = 5;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var current = await GetOrCreateAccountAsync(tenant, userKey, ct);

            var updated = mutation(current);

            if (ReferenceEquals(updated, current))
                return current;

            try
            {
                await UpdateAsync(updated, current.SecurityVersion, ct);

                return updated;
            }
            catch (UAuthConflictException) when (attempt < maxAttempts - 1)
            {
            }
        }

        throw new InvalidOperationException("Unreachable.");
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
