using CodeBeam.UltimateAuth.Core.Abstractions;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Sessions.InMemory
{
    public sealed class InMemorySessionStoreFactory : ISessionStoreFactory
    {
        private readonly ConcurrentDictionary<string, object> _stores = new();

        public ISessionStoreKernel<TUserId> Create<TUserId>(string? tenantId)
        {
            var key = tenantId ?? "__single__";

            var store = _stores.GetOrAdd(
                key,
                _ => new InMemorySessionStoreKernel<TUserId>());

            return (ISessionStoreKernel<TUserId>)store;
        }
    }
}
