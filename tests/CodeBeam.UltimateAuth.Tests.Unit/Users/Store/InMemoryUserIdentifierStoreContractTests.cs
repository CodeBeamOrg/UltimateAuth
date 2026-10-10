using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;
using CodeBeam.UltimateAuth.Users.InMemory;
using CodeBeam.UltimateAuth.Users.Reference;

public sealed class InMemoryUserIdentifierStoreContractTests : UserIdentifierStoreContractTests
{
    protected override Task<IUserIdentifierStoreTestDatabase> CreateDatabaseAsync()
    {
        return Task.FromResult<IUserIdentifierStoreTestDatabase>(
            new Database());
    }

    private sealed class Database : IUserIdentifierStoreTestDatabase
    {
        private readonly Dictionary<TenantKey, IUserIdentifierStore> _stores = [];

        private readonly InMemoryAtomicContextAccessor _atomicContext = new();

        public IUserIdentifierStore CreateStore(TenantKey tenant)
        {
            if (_stores.TryGetValue(tenant, out var existing))
                return existing;

            var store = new InMemoryUserIdentifierStore(
                new TenantExecutionContext(tenant),
                _atomicContext, new UAuthPaginationOptions());

            _stores.Add(tenant, store);

            return store;
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
