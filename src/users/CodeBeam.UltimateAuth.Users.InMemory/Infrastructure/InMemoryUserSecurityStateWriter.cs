using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSecurityStateWriter<TUserId> : IUserSecurityStateWriter<TUserId> where TUserId : notnull
{
    private readonly InMemoryUserSecurityStore<TUserId> _store;

    public InMemoryUserSecurityStateWriter(InMemoryUserSecurityStore<TUserId> store)
    {
        _store = store;
    }

    public Task RecordFailedLoginAsync(TenantKey tenant, TUserId userId, DateTimeOffset at, CancellationToken ct = default)
    {
        var current = _store.Get(tenant, userId);

        var next = new InMemoryUserSecurityState
        {
            SecurityVersion = (current?.SecurityVersion ?? 0) + 1,
            FailedLoginAttempts = (current?.FailedLoginAttempts ?? 0) + 1,
            LockedUntil = current?.LockedUntil,
            RequiresReauthentication = current?.RequiresReauthentication ?? false
        };

        _store.Set(tenant, userId, next);
        return Task.CompletedTask;
    }

    public Task LockUntilAsync(TenantKey tenant, TUserId userId, DateTimeOffset lockedUntil, CancellationToken ct = default)
    {
        var current = _store.Get(tenant, userId);

        var next = new InMemoryUserSecurityState
        {
            SecurityVersion = (current?.SecurityVersion ?? 0) + 1,
            FailedLoginAttempts = current?.FailedLoginAttempts ?? 0,
            LockedUntil = lockedUntil,
            RequiresReauthentication = current?.RequiresReauthentication ?? false
        };

        _store.Set(tenant, userId, next);
        return Task.CompletedTask;
    }

    public Task ResetFailuresAsync(TenantKey tenant, TUserId userId, CancellationToken ct = default)
    {
        _store.Clear(tenant, userId);
        return Task.CompletedTask;
    }
}
