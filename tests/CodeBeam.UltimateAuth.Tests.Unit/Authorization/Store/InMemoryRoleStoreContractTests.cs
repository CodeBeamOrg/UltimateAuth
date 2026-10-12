using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.InMemory;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.InMemory;

namespace CodeBeam.UltimateAuth.Tests.Contracts.Authorization;

public sealed class InMemoryRoleStoreContractTests : RoleStoreContractTests
{
    private readonly IUAuthPaginationPolicy _pagination = new UAuthPaginationOptions();

    protected override Task<IRoleStoreTestDatabase> CreateDatabaseAsync()
    {
        return Task.FromResult<IRoleStoreTestDatabase>(
            new InMemoryRoleStoreTestDatabase(_pagination));
    }

    private sealed class InMemoryRoleStoreTestDatabase : IRoleStoreTestDatabase
    {
        private readonly InMemoryAtomicContextAccessor _atomicContext = new();
        private readonly IUAuthPaginationPolicy _pagination;

        public InMemoryRoleStoreTestDatabase(IUAuthPaginationPolicy pagination)
        {
            _pagination = pagination;
        }

        public IRoleStore CreateStore(TenantKey tenant)
        {
            var executionContext = new TenantExecutionContext(tenant);

            return new InMemoryRoleStore(
                executionContext,
                _atomicContext, _pagination);
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
