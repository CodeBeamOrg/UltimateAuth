using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials.Contracts;

public interface IPasswordCredentialStoreTestDatabase : IAsyncDisposable
{
    IPasswordCredentialStore CreateStore(TenantKey tenant);
}
