using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserIdentifierStoreFactory : IUserIdentifierStoreFactory
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<TenantKey, InMemoryUserIdentifierStore> _stores = new();

    public InMemoryUserIdentifierStoreFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IUserIdentifierStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t =>
        {
            Console.WriteLine("New Store Added");
            var tenantContext = new TenantContext(tenant);
            return ActivatorUtilities.CreateInstance<InMemoryUserIdentifierStore>(_provider, tenantContext);
        });
    }
}
