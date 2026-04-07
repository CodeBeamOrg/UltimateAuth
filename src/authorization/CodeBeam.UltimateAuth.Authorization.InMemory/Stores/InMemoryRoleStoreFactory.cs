using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

public sealed class InMemoryRoleStoreFactory : IRoleStoreFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemoryRoleStore> _stores = new();

    public IRoleStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t => new InMemoryRoleStore(new TenantExecutionContext(t)));
    }
}
