using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IRefreshTokenStoreFactory
{
    IRefreshTokenStore Create(TenantKey tenant);
}
