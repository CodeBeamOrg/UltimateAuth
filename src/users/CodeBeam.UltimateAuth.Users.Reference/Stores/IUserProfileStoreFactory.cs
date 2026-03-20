using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserProfileStoreFactory
{
    IUserProfileStore Create(TenantKey tenant);
}
