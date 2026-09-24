using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tests.Unit.Authentication.Contracts;

public interface IAuthenticationSecurityStateStoreTestDatabase : IAsyncDisposable
{
    IAuthenticationSecurityStateStore CreateStore(TenantKey tenant);
}
