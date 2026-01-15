using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Stores
{
    /// <summary>
    /// UltimateAuth default session store factory.
    /// Resolves session store kernels from DI and provides them
    /// to framework-level session stores.
    /// </summary>
    public sealed class UAuthSessionStoreFactory : ISessionStoreKernelFactory
    {
        private readonly IServiceProvider _provider;

        public UAuthSessionStoreFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ISessionStoreKernel Create(string? tenantId)
        {
            var kernel = _provider.GetService<ISessionStoreKernel>();

            if (kernel is ITenantAwareSessionStore tenantAware)
            {
                tenantAware.BindTenant(tenantId);
            }

            return kernel;
        }

    }
}
