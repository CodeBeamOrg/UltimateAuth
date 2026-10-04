using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.InMemory;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

public sealed class InMemoryPasswordCredentialStoreFactory : IPasswordCredentialStoreFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemoryPasswordCredentialStore> _stores = new();
    private readonly InMemoryAtomicContextAccessor _atomicContext;

    public InMemoryPasswordCredentialStoreFactory(InMemoryAtomicContextAccessor atomicContext)
    {
        _atomicContext = atomicContext;
    }

    public IPasswordCredentialStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t => new InMemoryPasswordCredentialStore(new TenantExecutionContext(t), _atomicContext));
    }
}
