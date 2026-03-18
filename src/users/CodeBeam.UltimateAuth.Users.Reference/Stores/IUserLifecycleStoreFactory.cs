using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserLifecycleStoreFactory
{
    IUserLifecycleStore Create(TenantKey tenant);
}
