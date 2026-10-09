using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public interface IUserSummaryQueryStoreFactory
{
    IUserSummaryQueryStore Create(TenantKey tenant);
}
