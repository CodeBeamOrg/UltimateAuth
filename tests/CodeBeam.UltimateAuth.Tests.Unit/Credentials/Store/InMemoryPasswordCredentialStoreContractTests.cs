using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.InMemory;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.InMemory;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials.Contracts;

public sealed class InMemoryPasswordCredentialStoreContractTests : PasswordCredentialStoreContractTests
{
    protected override Task<IPasswordCredentialStoreTestDatabase>CreateDatabaseAsync()
    {
        return Task.FromResult<IPasswordCredentialStoreTestDatabase>(
            new Database());
    }

    private sealed class Database
    : IPasswordCredentialStoreTestDatabase
    {
        private readonly InMemoryAtomicContextAccessor _atomicContext = new();

        private readonly InMemoryPasswordCredentialStoreFactory _factory;

        public Database()
        {
            _factory = new InMemoryPasswordCredentialStoreFactory(
                _atomicContext);
        }

        public IPasswordCredentialStore CreateStore(TenantKey tenant)
            => _factory.Create(tenant);

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
