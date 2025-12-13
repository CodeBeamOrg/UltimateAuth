using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Stores
{
    /// <summary>
    /// UltimateAuth default session store factory.
    /// Resolves session store kernels from DI and provides them
    /// to framework-level session stores.
    /// </summary>
    public sealed class UAuthSessionStoreFactory : ISessionStoreFactory
    {
        private readonly IServiceProvider _provider;

        public UAuthSessionStoreFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ISessionStoreKernel<TUserId> Create<TUserId>(string? tenantId)
        {
            var kernel = _provider.GetService<ISessionStoreKernel<TUserId>>();

            if (kernel is null)
            {
                throw new InvalidOperationException(
                    "No ISessionStoreKernel<TUserId> registered. " +
                    "Call AddUltimateAuthServer().AddSessionStoreKernel<TKernel, TUserId>()."
                );
            }

            return kernel;
        }

    }
}
