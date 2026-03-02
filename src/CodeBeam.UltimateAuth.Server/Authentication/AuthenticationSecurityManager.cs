using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
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
        var state = await _store.GetAsync(tenant, userKey, AuthenticationSecurityScope.Account, credentialType: null, ct);

        if (state is not null)
            return state;

        var created = AuthenticationSecurityState.CreateAccount(tenant, userKey);
        await _store.AddAsync(created, ct);
        return created;
    }

    public async Task<AuthenticationSecurityState> GetOrCreateFactorAsync(TenantKey tenant, UserKey userKey, CredentialType type, CancellationToken ct = default)
    {
        var state = await _store.GetAsync(tenant, userKey, AuthenticationSecurityScope.Factor, type, ct);

        if (state is not null)
            return state;

        var created = AuthenticationSecurityState.CreateFactor(tenant, userKey, type);
        await _store.AddAsync(created, ct);
        return created;
    }

    public async Task<AuthenticationSecurityState> RegisterFailureAsync(AuthenticationSecurityState state, DateTimeOffset now, CancellationToken ct = default)
    {
        var loginOptions = _options.Login;

        var threshold = loginOptions.MaxFailedAttempts;
        var duration = loginOptions.LockoutDuration;
        var window = loginOptions.FailureWindow;
        var extendLock = loginOptions.ExtendLockOnFailure;

        if (window > TimeSpan.Zero)
        {
            state = state.ResetFailuresIfWindowExpired(now, window);
        }

        var updated = state.RegisterFailure(now, threshold, duration, extendLock: extendLock);
        await PersistWithRetryAsync(updated, state.SecurityVersion, ct);
        return updated;
    }

    public async Task<AuthenticationSecurityState> RegisterSuccessAsync(AuthenticationSecurityState state, CancellationToken ct = default)
    {
        var updated = state.RegisterSuccess();
        await PersistWithRetryAsync(updated, state.SecurityVersion, ct);
        return updated;
    }

    public async Task<AuthenticationSecurityState> UnlockAsync(AuthenticationSecurityState state, CancellationToken ct = default)
    {
        var updated = state.Unlock();
        await PersistWithRetryAsync(updated, state.SecurityVersion, ct);
        return updated;
    }

    private async Task PersistWithRetryAsync(AuthenticationSecurityState updated, long expectedVersion, CancellationToken ct)
    {
        const int maxRetries = 3;

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await _store.UpdateAsync(updated, expectedVersion, ct);
                return;
            }
            catch (InvalidOperationException)
            {
                if (i == maxRetries - 1)
                    throw;

                var current = await _store.GetAsync(updated.Tenant, updated.UserKey, updated.Scope, updated.CredentialType, ct);

                if (current is null)
                    throw;

                updated = current;
                expectedVersion = current.SecurityVersion;
            }
        }
    }
}