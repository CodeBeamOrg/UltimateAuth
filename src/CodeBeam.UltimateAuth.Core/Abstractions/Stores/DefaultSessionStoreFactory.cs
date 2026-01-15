using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Default session store factory that throws until a real store implementation is registered.
    /// </summary>
    internal sealed class DefaultSessionStoreFactory : ISessionStoreKernelFactory
    {
        private readonly IServiceProvider _sp;

        public DefaultSessionStoreFactory(IServiceProvider sp)
        {
            _sp = sp;
        }

        public ISessionStoreKernel Create(string? tenantId)
            => _sp.GetRequiredService<ISessionStoreKernel>();
    }
}
