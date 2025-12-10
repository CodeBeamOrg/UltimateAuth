namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionStoreFactory
    {
        ISessionStore<TUserId> Create<TUserId>(string? tenantId);
    }
}
