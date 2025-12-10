namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public sealed class DefaultSessionStoreFactory : ISessionStoreFactory
    {
        public ISessionStore<TUserId> Create<TUserId>(string? tenantId)
        {
            throw new InvalidOperationException(
                "No ISessionStore<TUserId> implementation registered. " +
                "Call AddUltimateAuthServer().AddSessionStore<TStore>() to provide a real implementation."
            );
        }
    }
}
