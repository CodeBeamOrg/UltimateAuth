using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.InMemory;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials.Contracts;

public sealed class InMemoryPasswordCredentialStoreContractTests
    : PasswordCredentialStoreContractTests
{
    protected override Task<IPasswordCredentialStoreTestDatabase>
        CreateDatabaseAsync()
    {
        return Task.FromResult<IPasswordCredentialStoreTestDatabase>(
            new Database());
    }

    private sealed class Database
        : IPasswordCredentialStoreTestDatabase
    {
        private readonly InMemoryPasswordCredentialStoreFactory _factory =
            new();

        public IPasswordCredentialStore CreateStore(TenantKey tenant)
            => _factory.Create(tenant);

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
