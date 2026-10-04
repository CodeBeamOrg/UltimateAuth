using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.InMemory;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

public sealed class InMemoryRoleStoreFactory : IRoleStoreFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemoryRoleStore> _stores = new();
    private readonly InMemoryAtomicContextAccessor _atomicContext;

    public InMemoryRoleStoreFactory(InMemoryAtomicContextAccessor atomicContext)
    {
        _atomicContext = atomicContext;
    }

    public IRoleStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t => new InMemoryRoleStore(new TenantExecutionContext(t), _atomicContext));
    }
}
