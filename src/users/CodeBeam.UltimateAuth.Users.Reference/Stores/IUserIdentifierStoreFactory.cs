using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserIdentifierStoreFactory
{
    IUserIdentifierStore Create(TenantKey tenant);
}