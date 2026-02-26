using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSecurityStateWriter : IUserSecurityStateWriter
{
    private readonly InMemoryUserSecurityStore _store;

    public InMemoryUserSecurityStateWriter(InMemoryUserSecurityStore store)
    {
        _store = store;
    }

    public Task RecordFailedLoginAsync(TenantKey tenant, UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        var current = _store.Get(tenant, userKey);

        var next = new InMemoryUserSecurityState
        {
            SecurityVersion = (current?.SecurityVersion ?? 0) + 1,
            FailedLoginAttempts = (current?.FailedLoginAttempts ?? 0) + 1,
            LockedUntil = current?.LockedUntil,
            RequiresReauthentication = current?.RequiresReauthentication ?? false,
            LastFailedAt = at
        };

        _store.Set(tenant, userKey, next);
        return Task.CompletedTask;
    }

    public Task LockUntilAsync(TenantKey tenant, UserKey userKey, DateTimeOffset lockedUntil, CancellationToken ct = default)
    {
        var current = _store.Get(tenant, userKey);

        var next = new InMemoryUserSecurityState
        {
            SecurityVersion = (current?.SecurityVersion ?? 0) + 1,
            FailedLoginAttempts = current?.FailedLoginAttempts ?? 0,
            LockedUntil = lockedUntil,
            RequiresReauthentication = current?.RequiresReauthentication ?? false
        };

        _store.Set(tenant, userKey, next);
        return Task.CompletedTask;
    }

    public Task ResetFailuresAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        _store.Clear(tenant, userKey);
        return Task.CompletedTask;
    }
}
