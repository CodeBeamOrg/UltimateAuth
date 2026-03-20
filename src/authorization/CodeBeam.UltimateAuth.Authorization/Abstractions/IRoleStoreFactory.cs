using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public interface IRoleStoreFactory
{
    IRoleStore Create(TenantKey tenant);
}
