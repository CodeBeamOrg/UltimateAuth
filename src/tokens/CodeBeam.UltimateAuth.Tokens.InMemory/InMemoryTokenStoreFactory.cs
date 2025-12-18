using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Tokens.InMemory;

internal sealed class InMemoryTokenStoreFactory : ITokenStoreFactory
{
    private readonly ConcurrentDictionary<string, ITokenStoreKernel> _kernels = new();

    public ITokenStoreKernel Create(string? tenantId)
    {
        var key = tenantId ?? "__single__";

        return _kernels.GetOrAdd(
            key,
            _ => new InMemoryTokenStoreKernel());
    }
}
