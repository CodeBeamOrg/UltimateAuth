using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tests.Unit.Tokens.Contracts;

public interface IRefreshTokenStoreTestDatabase : IAsyncDisposable
{
    IRefreshTokenStore CreateStore(TenantKey tenant);
}
