using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tests.Unit.Sessions.Contracts;

public interface ISessionStoreTestDatabase : IAsyncDisposable
{
    ISessionStore CreateStore(TenantKey tenant);
}
