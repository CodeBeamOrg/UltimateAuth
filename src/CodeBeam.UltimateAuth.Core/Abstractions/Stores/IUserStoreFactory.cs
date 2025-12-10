namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IUserStoreFactory
    {
        IUserStore<TUserId> Create<TUserId>(string tenantId);
    }
}
