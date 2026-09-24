using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.InMemory;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tests.Unit.Authorization.Contracts;

public sealed class InMemoryUserRoleStoreContractTests : UserRoleStoreContractTests
{
    protected override Task<IUserRoleStoreTestDatabase>
        CreateDatabaseAsync()
    {
        return Task.FromResult<IUserRoleStoreTestDatabase>(
            new InMemoryUserRoleStoreTestDatabase());
    }

    private sealed class InMemoryUserRoleStoreTestDatabase : IUserRoleStoreTestDatabase
    {
        private readonly Dictionary<TenantKey, IUserRoleStore>
            _stores = [];

        public IUserRoleStore CreateStore(TenantKey tenant)
        {
            if (_stores.TryGetValue(tenant, out var store))
                return store;

            store = new InMemoryUserRoleStore(
                new TenantExecutionContext(tenant));

            _stores.Add(tenant, store);

            return store;
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}