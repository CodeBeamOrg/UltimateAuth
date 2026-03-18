using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Reference;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Credentials.InMemory;

public sealed class InMemoryPasswordCredentialStoreFactory : IPasswordCredentialStoreFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemoryPasswordCredentialStore> _stores = new();

    public IPasswordCredentialStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, t => new InMemoryPasswordCredentialStore(new TenantContext(t)));
    }
}
