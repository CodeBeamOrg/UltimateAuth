using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tests.Contracts.Authorization;

public interface IRoleStoreTestDatabase : IAsyncDisposable
{
    IRoleStore CreateStore(TenantKey tenant);
}
