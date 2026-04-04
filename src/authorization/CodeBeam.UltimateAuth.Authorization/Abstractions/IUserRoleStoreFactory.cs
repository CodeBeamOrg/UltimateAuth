using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IUserRoleStoreFactory
{
    IUserRoleStore Create(TenantKey tenant);
}
