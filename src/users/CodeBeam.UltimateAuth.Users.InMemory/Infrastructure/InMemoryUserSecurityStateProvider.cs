using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSecurityStateProvider : IUserSecurityStateProvider
{
    private readonly InMemoryUserSecurityStore _store;

    public InMemoryUserSecurityStateProvider(InMemoryUserSecurityStore store)
    {
        _store = store;
    }

    public Task<IUserSecurityState?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        return Task.FromResult<IUserSecurityState?>(_store.Get(tenant, userKey));
    }
}
