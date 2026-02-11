using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSecurityStateProvider<TUserId> : IUserSecurityStateProvider<TUserId> where TUserId : notnull
{
    private readonly InMemoryUserSecurityStore<TUserId> _store;

    public InMemoryUserSecurityStateProvider(InMemoryUserSecurityStore<TUserId> store)
    {
        _store = store;
    }

    public Task<IUserSecurityState?> GetAsync(TenantKey tenant, TUserId userId, CancellationToken ct = default)
    {
        return Task.FromResult<IUserSecurityState?>(_store.Get(tenant, userId));
    }
}
