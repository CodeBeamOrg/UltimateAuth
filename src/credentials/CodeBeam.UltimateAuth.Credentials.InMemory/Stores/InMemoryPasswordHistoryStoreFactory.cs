using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.InMemory;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

public sealed class InMemoryPasswordHistoryStoreFactory(InMemoryAtomicContextAccessor atomicContext) : IPasswordHistoryStoreFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemoryPasswordHistoryStore> _stores = new();

    public IPasswordHistoryStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t => new InMemoryPasswordHistoryStore(t, atomicContext));
    }
}
