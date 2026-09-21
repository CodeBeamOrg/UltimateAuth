using CodeBeam.UltimateAuth.Authentication.InMemory;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tests.Unit.Authentication.Contracts;

public sealed class InMemoryAuthenticationSecurityStateStoreContractTests : AuthenticationSecurityStateStoreContractTests
{
    protected override Task<IAuthenticationSecurityStateStoreTestDatabase>
        CreateDatabaseAsync()
    {
        return Task.FromResult<IAuthenticationSecurityStateStoreTestDatabase>(
            new Database());
    }

    private sealed class Database
        : IAuthenticationSecurityStateStoreTestDatabase
    {
        private readonly Dictionary<
            TenantKey,
            IAuthenticationSecurityStateStore> _stores = [];

        public IAuthenticationSecurityStateStore CreateStore(
            TenantKey tenant)
        {
            if (_stores.TryGetValue(tenant, out var store))
                return store;

            store = new InMemoryAuthenticationSecurityStateStore(
                new TenantExecutionContext(tenant));

            _stores.Add(tenant, store);

            return store;
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
