using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Authentication.InMemory;

internal sealed class InMemoryAuthenticationSecurityStateStoreFactory : IAuthenticationSecurityStateStoreFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemoryAuthenticationSecurityStateStore> _stores = new();

    public IAuthenticationSecurityStateStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t => new InMemoryAuthenticationSecurityStateStore(new TenantContext(t)));
    }
}
