using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.InMemory
{
    internal sealed class InMemorySessionActivityWriter<TUserId> : ISessionActivityWriter<TUserId> where TUserId : notnull
    {
        private readonly ISessionStoreFactory _factory;

        public InMemorySessionActivityWriter(ISessionStoreFactory factory)
        {
            _factory = factory;
        }

        public Task TouchAsync(string? tenantId, ISession<TUserId> session, CancellationToken ct)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            return kernel.SaveSessionAsync(tenantId, session);
        }
    }

}
