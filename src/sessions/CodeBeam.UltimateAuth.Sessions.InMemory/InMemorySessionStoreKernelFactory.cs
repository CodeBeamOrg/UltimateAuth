using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Sessions.InMemory;

public sealed class InMemorySessionStoreKernelFactory : ISessionStoreKernelFactory
{
    private readonly ConcurrentDictionary<TenantKey, InMemorySessionStoreKernel> _kernels = new();

    public ISessionStoreKernel Create(TenantKey tenant)
    {
        return _kernels.GetOrAdd(tenant, _ => new InMemorySessionStoreKernel());
    }
}
