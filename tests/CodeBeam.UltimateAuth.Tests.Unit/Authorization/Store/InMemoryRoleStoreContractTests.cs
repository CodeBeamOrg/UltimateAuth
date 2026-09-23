using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.InMemory;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;

namespace CodeBeam.UltimateAuth.Tests.Contracts.Authorization;

public sealed class InMemoryRoleStoreContractTests : RoleStoreContractTests
{
    protected override Task<IRoleStoreTestDatabase> CreateDatabaseAsync()
    {
        return Task.FromResult<IRoleStoreTestDatabase>(
            new InMemoryRoleStoreTestDatabase());
    }

    private sealed class InMemoryRoleStoreTestDatabase
        : IRoleStoreTestDatabase
    {
        public IRoleStore CreateStore(TenantKey tenant)
        {
            var executionContext =
                new TenantExecutionContext(tenant);

            return new InMemoryRoleStore(
                executionContext);
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}