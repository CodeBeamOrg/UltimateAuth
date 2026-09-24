using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tests.Unit.Authorization.Contracts;

public interface IUserRoleStoreTestDatabase : IAsyncDisposable
{
    IUserRoleStore CreateStore(TenantKey tenant);
}