using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public interface IUserIdentifierStoreTestDatabase : IAsyncDisposable
{
    IUserIdentifierStore CreateStore(TenantKey tenant);
}
