using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

/// <summary>
/// Default session store factory that throws until a real store implementation is registered.
/// </summary>
internal sealed class UAuthSessionStoreKernelFactory : ISessionStoreKernelFactory
{
    private readonly IServiceProvider _sp;

    public UAuthSessionStoreKernelFactory(IServiceProvider sp)
    {
        _sp = sp;
    }

    public ISessionStoreKernel Create(string? tenantId) => _sp.GetRequiredService<ISessionStoreKernel>();
}
