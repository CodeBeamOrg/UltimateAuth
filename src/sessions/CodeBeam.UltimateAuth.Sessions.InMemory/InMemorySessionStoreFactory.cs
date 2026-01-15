using CodeBeam.UltimateAuth.Core.Abstractions;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Sessions.InMemory
{
    public sealed class InMemorySessionStoreFactory : ISessionStoreKernelFactory
    {
        private readonly ConcurrentDictionary<string, object> _stores = new();

        public ISessionStoreKernel Create(string? tenantId)
        {
            var key = tenantId ?? "__single__";

            var store = _stores.GetOrAdd(key, _ =>
            {
                var k = new InMemorySessionStoreKernel();
                k.BindTenant(tenantId);
                return k;
            });

            return (ISessionStoreKernel)store;
        }
    }
}
