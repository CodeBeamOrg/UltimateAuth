using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal class NotSupportedRefreshTokenStoreFactory : IRefreshTokenStoreFactory
{
    public IRefreshTokenStore Create(TenantKey tenant)
    {
        throw new NotSupportedException();
    }
}
