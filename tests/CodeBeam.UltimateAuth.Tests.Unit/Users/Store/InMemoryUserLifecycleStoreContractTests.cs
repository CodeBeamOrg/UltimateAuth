using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.InMemory;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public sealed class InMemoryUserLifecycleStoreContractTests
    : UserLifecycleStoreContractTests
{
    protected override Task<IUserLifecycleStoreTestDatabase>
        CreateDatabaseAsync()
    {
        return Task.FromResult<IUserLifecycleStoreTestDatabase>(
            new Database());
    }

    private sealed class Database : IUserLifecycleStoreTestDatabase
    {
        private readonly Dictionary<TenantKey, IUserLifecycleStore> _stores = [];

        public IUserLifecycleStore CreateStore(TenantKey tenant)
        {
            if (_stores.TryGetValue(tenant, out var existing))
                return existing;

            var store = new InMemoryUserLifecycleStore(
                new TenantExecutionContext(tenant));

            _stores.Add(tenant, store);

            return store;
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
