using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserLifecycleStoreFactory : IUserLifecycleStoreFactory
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<TenantKey, InMemoryUserLifecycleStore> _stores = new();

    public InMemoryUserLifecycleStoreFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IUserLifecycleStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t =>
        {
            var tenantContext = new TenantExecutionContext(tenant);
            return ActivatorUtilities.CreateInstance<InMemoryUserLifecycleStore>(_provider, tenantContext);
        });
    }
}
