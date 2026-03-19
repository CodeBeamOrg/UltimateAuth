using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal class NotSupportedUserRoleStoreFactory : IUserRoleStoreFactory
{
    public IUserRoleStore Create(TenantKey tenant)
    {
        throw new NotSupportedException();
    }
}
