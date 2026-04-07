using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

public sealed class InMemoryUserRoleStoreFactory : IUserRoleStoreFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemoryUserRoleStore> _stores = new();

    public IUserRoleStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t => new InMemoryUserRoleStore(new TenantExecutionContext(t)));
    }
}
