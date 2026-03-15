using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tokens.InMemory;

public sealed class InMemoryRefreshTokenStoreFactory : IRefreshTokenStoreFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemoryRefreshTokenStore> _stores = new();

    public IRefreshTokenStore Create(TenantKey tenant)
    {
        return _stores.GetOrAdd(tenant, _ => new InMemoryRefreshTokenStore(tenant));
    }
}