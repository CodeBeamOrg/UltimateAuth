using CodeBeam.UltimateAuth.Core.Abstractions;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Sessions.InMemory;

public sealed class InMemorySessionStoreKernelFactory : ISessionStoreKernelFactory
{
    private readonly ConcurrentDictionary<string, InMemorySessionStoreKernel> _kernels = new();

    public ISessionStoreKernel Create(string? tenantId)
    {
        //var key = TenantKey.Normalize(tenantId);
        var key = tenantId ?? "__default__";

        return _kernels.GetOrAdd(key, _ => new InMemorySessionStoreKernel());
    }
}
