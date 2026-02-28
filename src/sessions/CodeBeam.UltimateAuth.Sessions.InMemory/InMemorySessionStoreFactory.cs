using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Sessions.InMemory;

public sealed class InMemorySessionStoreFactory : ISessionStoreFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemorySessionStore> _kernels = new();

    public ISessionStore Create(TenantKey tenant)
    {
        return _kernels.GetOrAdd(tenant, _ => new InMemorySessionStore());
    }
}
