using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.InMemory;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public sealed class InMemoryUserProfileStoreContractTests
    : UserProfileStoreContractTests
{
    protected override Task<IUserProfileStoreTestDatabase>
        CreateDatabaseAsync()
    {
        return Task.FromResult<IUserProfileStoreTestDatabase>(
            new Database());
    }

    private sealed class Database : IUserProfileStoreTestDatabase
    {
        private readonly Dictionary<TenantKey, IUserProfileStore> _stores = [];

        public IUserProfileStore CreateStore(TenantKey tenant)
        {
            if (_stores.TryGetValue(tenant, out var existing))
                return existing;

            var store = new InMemoryUserProfileStore(
                new TenantExecutionContext(tenant));

            _stores.Add(tenant, store);

            return store;
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
