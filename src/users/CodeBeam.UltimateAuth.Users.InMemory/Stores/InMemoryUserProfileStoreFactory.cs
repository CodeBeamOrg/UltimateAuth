using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserProfileStoreFactory : IUserProfileStoreFactory
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<TenantKey, InMemoryUserProfileStore> _stores = new();

    public InMemoryUserProfileStoreFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IUserProfileStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t =>
        {
            var tenantContext = new TenantContext(t);
            return ActivatorUtilities.CreateInstance<InMemoryUserProfileStore>(_provider, tenantContext);
        });
    }
}
