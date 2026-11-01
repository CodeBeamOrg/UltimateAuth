using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface IPasswordHistoryStoreFactory
{
    IPasswordHistoryStore Create(TenantKey tenant);
}
