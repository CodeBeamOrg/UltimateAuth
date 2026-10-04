using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.InMemory;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;

namespace CodeBeam.UltimateAuth.Tests.Contracts.Authorization;

public sealed class InMemoryRoleStoreContractTests : RoleStoreContractTests
{
    protected override Task<IRoleStoreTestDatabase> CreateDatabaseAsync()
    {
        return Task.FromResult<IRoleStoreTestDatabase>(
            new InMemoryRoleStoreTestDatabase());
    }

    private sealed class InMemoryRoleStoreTestDatabase : IRoleStoreTestDatabase
    {
        private readonly InMemoryAtomicContextAccessor _atomicContext = new();

        public IRoleStore CreateStore(TenantKey tenant)
        {
            var executionContext = new TenantExecutionContext(tenant);

            return new InMemoryRoleStore(
                executionContext,
                _atomicContext);
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}