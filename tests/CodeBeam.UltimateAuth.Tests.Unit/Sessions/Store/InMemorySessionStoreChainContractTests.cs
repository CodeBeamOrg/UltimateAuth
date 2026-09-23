using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Sessions.InMemory;

namespace CodeBeam.UltimateAuth.Tests.Unit.Sessions.Contracts;

public sealed class InMemorySessionStoreChainContractTests
    : SessionStoreChainContractTests
{
    protected override Task<ISessionStoreTestDatabase>
        CreateDatabaseAsync()
    {
        return Task.FromResult<ISessionStoreTestDatabase>(
            new Database());
    }

    private sealed class Database
        : ISessionStoreTestDatabase
    {
        private readonly InMemorySessionStoreFactory _factory =
            new();

        public ISessionStore CreateStore(TenantKey tenant)
            => _factory.Create(tenant);

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
