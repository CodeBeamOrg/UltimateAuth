using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    public sealed class EfCoreSessionStoreKernelFactory : ISessionStoreKernelFactory
    {
        private readonly IServiceProvider _sp;

        public EfCoreSessionStoreKernelFactory(IServiceProvider sp)
        {
            _sp = sp;
        }

        public ISessionStoreKernel Create(string? tenantId)
        {
            return _sp.GetRequiredService<EfCoreSessionStoreKernel>();
        }
    }
}
