using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.InMemory;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public sealed class InMemoryUserIdentifierStoreContractTests
    : UserIdentifierStoreContractTests
{
    protected override Task<IUserIdentifierStoreTestDatabase>
        CreateDatabaseAsync()
    {
        return Task.FromResult<IUserIdentifierStoreTestDatabase>(
            new Database());
    }

    private sealed class Database : IUserIdentifierStoreTestDatabase
    {
        private readonly Dictionary<TenantKey, IUserIdentifierStore> _stores = [];

        public IUserIdentifierStore CreateStore(TenantKey tenant)
        {
            if (_stores.TryGetValue(tenant, out var store))
                return store;

            store = new InMemoryUserIdentifierStore(
                new TenantExecutionContext(tenant));

            _stores.Add(tenant, store);

            return store;
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
