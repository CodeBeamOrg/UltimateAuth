using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tokens.InMemory;

namespace CodeBeam.UltimateAuth.Tests.Unit.Tokens.Contracts;

public sealed class InMemoryRefreshTokenStoreContractTests
    : RefreshTokenStoreContractTests
{
    protected override Task<IRefreshTokenStoreTestDatabase>
        CreateDatabaseAsync()
    {
        return Task.FromResult<IRefreshTokenStoreTestDatabase>(
            new Database());
    }

    private sealed class Database
        : IRefreshTokenStoreTestDatabase
    {
        private readonly InMemoryRefreshTokenStoreFactory _factory =
            new();

        public IRefreshTokenStore CreateStore(TenantKey tenant)
            => _factory.Create(tenant);

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
