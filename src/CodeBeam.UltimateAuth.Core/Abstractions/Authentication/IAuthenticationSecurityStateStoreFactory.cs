using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IAuthenticationSecurityStateStoreFactory
    {
        IAuthenticationSecurityStateStore Create(TenantKey tenant);
    }
}
