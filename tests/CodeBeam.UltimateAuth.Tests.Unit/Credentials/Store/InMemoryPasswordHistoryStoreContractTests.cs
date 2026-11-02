using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.InMemory;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.InMemory;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials.Contracts;

public sealed class InMemoryPasswordHistoryStoreContractTests : PasswordHistoryStoreContractTests
{
    protected override Task<IPasswordHistoryStoreTestDatabase> CreateDatabaseAsync()
    {
        return Task.FromResult<IPasswordHistoryStoreTestDatabase>(new Database());
    }

    private sealed class Database : IPasswordHistoryStoreTestDatabase
    {
        private readonly InMemoryAtomicContextAccessor
            _atomicContext = new();

        private readonly InMemoryPasswordHistoryStoreFactory
            _factory;

        public Database()
        {
            _factory = new InMemoryPasswordHistoryStoreFactory(
                _atomicContext);
        }

        public IPasswordHistoryStore CreateStore(TenantKey tenant)
            => _factory.Create(tenant);

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
