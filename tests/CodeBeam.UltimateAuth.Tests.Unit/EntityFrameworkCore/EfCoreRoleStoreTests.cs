using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Data.Sqlite;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EfCoreRoleStoreTests : EfCoreTestBase
{
    private static UAuthAuthorizationDbContext CreateDb(SqliteConnection connection)
    {
        return CreateDbContext<UAuthAuthorizationDbContext>(connection, options => new UAuthAuthorizationDbContext(options));
    }

    [Fact]
    public async Task Add_And_Get_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

        var role = Role.Create(
            null,
            tenant,
            "admin",
            new[] { Permission.From("read"), Permission.From("write") },
            DateTimeOffset.UtcNow);

        await store.AddAsync(role);

        var result = await store.GetAsync(new RoleKey(tenant, role.Id));

        Assert.NotNull(result);
        Assert.Equal("admin", result!.Name);
        Assert.Equal(2, result.Permissions.Count);
    }

    [Fact]
    public async Task Add_With_Duplicate_Name_Should_Throw()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

        var role1 = Role.Create(null, tenant, "admin", null, DateTimeOffset.UtcNow);
        var role2 = Role.Create(null, tenant, "ADMIN", null, DateTimeOffset.UtcNow);

        await store.AddAsync(role1);

        await Assert.ThrowsAsync<UAuthConflictException>(() => store.AddAsync(role2));
    }

    [Fact]
    public async Task Save_Should_Increment_Version()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        RoleId roleId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var role = Role.Create(null, tenant, "admin", null, DateTimeOffset.UtcNow);
            roleId = role.Id;
            await store.AddAsync(role);
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var existing = await store.GetAsync(new RoleKey(tenant, roleId));
            var updated = existing!.Rename("admin2", DateTimeOffset.UtcNow);
            await store.SaveAsync(updated, expectedVersion: 0);
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var result = await store.GetAsync(new RoleKey(tenant, roleId));

            Assert.Equal(1, result!.Version);
            Assert.Equal("admin2", result.Name);
        }
    }

    [Fact]
    public async Task Save_With_Wrong_Version_Should_Throw()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        RoleId roleId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var role = Role.Create(null, tenant, "admin", null, DateTimeOffset.UtcNow);
            roleId = role.Id;
            await store.AddAsync(role);
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var existing = await store.GetAsync(new RoleKey(tenant, roleId));
            var updated = existing!.Rename("admin2", DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<UAuthConcurrencyException>(() => store.SaveAsync(updated, expectedVersion: 999));
        }
    }

    [Fact]
    public async Task Rename_To_Existing_Name_Should_Throw()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;

        RoleId role1Id;
        RoleId role2Id;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var role1 = Role.Create(null, tenant, "admin", null, DateTimeOffset.UtcNow);
            var role2 = Role.Create(null, tenant, "user", null, DateTimeOffset.UtcNow);
            role1Id = role1.Id;
            role2Id = role2.Id;
            await store.AddAsync(role1);
            await store.AddAsync(role2);
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var role = await store.GetAsync(new RoleKey(tenant, role2Id));
            var updated = role!.Rename("admin", DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<UAuthConflictException>(() => store.SaveAsync(updated, 0));
        }
    }

    [Fact]
    public async Task Save_Should_Replace_Permissions()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        RoleId roleId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

            var role = Role.Create(
                null,
                tenant,
                "admin",
                new[] { Permission.From(UAuthActions.Authorization.Roles.GetAdmin) },
                DateTimeOffset.UtcNow);

            roleId = role.Id;

            await store.AddAsync(role);
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var existing = await store.GetAsync(new RoleKey(tenant, roleId));
            var updated = existing!.SetPermissions(
                new[]
                {
                    Permission.From(UAuthActions.Authorization.Roles.SetPermissionsAdmin)
                },
                DateTimeOffset.UtcNow);
            await store.SaveAsync(updated, 0);
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var result = await store.GetAsync(new RoleKey(tenant, roleId));

            Assert.Single(result!.Permissions);
            Assert.Contains(result.Permissions, p => p.Value == UAuthActions.Authorization.Roles.SetPermissionsAdmin);
        }
    }

    [Fact]
    public async Task Soft_Delete_Should_Work()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        RoleId roleId;

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var role = Role.Create(null, tenant, "admin", null, DateTimeOffset.UtcNow);
            roleId = role.Id;
            await store.AddAsync(role);
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            await store.DeleteAsync(new RoleKey(tenant, roleId), 0, DeleteMode.Soft, DateTimeOffset.UtcNow);
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));
            var result = await store.GetAsync(new RoleKey(tenant, roleId));
            Assert.NotNull(result!.DeletedAt);
        }
    }

    [Fact]
    public async Task Query_Should_Filter_And_Page()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreRoleStore<UAuthAuthorizationDbContext>(db, new TenantExecutionContext(tenant));

        await store.AddAsync(Role.Create(null, tenant, "admin", null, DateTimeOffset.UtcNow));
        await store.AddAsync(Role.Create(null, tenant, "user", null, DateTimeOffset.UtcNow));
        await store.AddAsync(Role.Create(null, tenant, "guest", null, DateTimeOffset.UtcNow));

        var result = await store.QueryAsync(new RoleQuery
        {
            Search = "us",
            PageNumber = 1,
            PageSize = 10
        });

        Assert.Single(result.Items);
        Assert.Equal("user", result.Items.First().Name);
    }
}
