using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Data.Sqlite;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EfCoreUserRoleStoreTests : EfCoreTestBase
{
    private static UAuthAuthorizationDbContext CreateDb(SqliteConnection connection)
    {
        return CreateDbContext<UAuthAuthorizationDbContext>(connection, options => new UAuthAuthorizationDbContext(options));
    }

    [Fact]
    public async Task Assign_And_GetAssignments_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var roleId = RoleId.New();

        await store.AssignAsync(userKey, roleId, DateTimeOffset.UtcNow);
        var result = await store.GetAssignmentsAsync(userKey);

        Assert.Single(result);
        Assert.Equal(roleId, result.First().RoleId);
    }

    [Fact]
    public async Task Assign_Duplicate_Should_Throw()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var roleId = RoleId.New();

        await store.AssignAsync(userKey, roleId, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<UAuthConflictException>(() => store.AssignAsync(userKey, roleId, DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Remove_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var roleId = RoleId.New();

        await store.AssignAsync(userKey, roleId, DateTimeOffset.UtcNow);
        await store.RemoveAsync(userKey, roleId);
        var result = await store.GetAssignmentsAsync(userKey);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Remove_NonExisting_Should_Not_Throw()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var roleId = RoleId.New();

        await store.RemoveAsync(userKey, roleId); // should not throw
    }

    [Fact]
    public async Task CountAssignments_Should_Return_Correct_Count()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

        var roleId = RoleId.New();

        await store.AssignAsync(UserKey.FromGuid(Guid.NewGuid()), roleId, DateTimeOffset.UtcNow);
        await store.AssignAsync(UserKey.FromGuid(Guid.NewGuid()), roleId, DateTimeOffset.UtcNow);

        var count = await store.CountAssignmentsAsync(roleId);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RemoveAssignmentsByRole_Should_Remove_All()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

        var roleId = RoleId.New();

        var user1 = UserKey.FromGuid(Guid.NewGuid());
        var user2 = UserKey.FromGuid(Guid.NewGuid());

        await store.AssignAsync(user1, roleId, DateTimeOffset.UtcNow);
        await store.AssignAsync(user2, roleId, DateTimeOffset.UtcNow);

        await store.RemoveAssignmentsByRoleAsync(roleId);

        var count = await store.CountAssignmentsAsync(roleId);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Should_Not_See_Data_From_Other_Tenant()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant1 = TenantKeys.Single;
        var tenant2 = TenantKey.FromInternal("tenant-2");

        var store1 = new EfCoreUserRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant1));
        var store2 = new EfCoreUserRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant2));

        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var roleId = RoleId.New();

        await store1.AssignAsync(userKey, roleId, DateTimeOffset.UtcNow);

        var result = await store2.GetAssignmentsAsync(userKey);

        Assert.Empty(result);
    }
}
