using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Data.Sqlite;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EfCoreUserLifecycleStoreTests : EfCoreTestBase
{
    private static UAuthUserDbContext CreateDb(SqliteConnection connection)
    {
        return CreateDbContext<UAuthUserDbContext>(connection, options => new UAuthUserDbContext(options));
    }

    [Fact]
    public async Task Add_And_Get_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserLifecycleStore(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var lifecycle = UserLifecycle.Create(
            tenant,
            userKey,
            DateTimeOffset.UtcNow);

        await store.AddAsync(lifecycle);

        var result = await store.GetAsync(new UserLifecycleKey(tenant, userKey));

        Assert.NotNull(result);
        Assert.Equal(userKey, result!.UserKey);
        Assert.Equal(UserStatus.Active, result.Status);
    }

    [Fact]
    public async Task Exists_Should_Return_True_When_Exists()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserLifecycleStore(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var lifecycle = UserLifecycle.Create(
            tenant,
            userKey,
            DateTimeOffset.UtcNow);

        await store.AddAsync(lifecycle);
        var exists = await store.ExistsAsync(new UserLifecycleKey(tenant, userKey));

        Assert.True(exists);
    }

    [Fact]
    public async Task Save_Should_Increment_Version()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var lifecycle = UserLifecycle.Create(
            tenant,
            userKey,
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCoreUserLifecycleStore(db1, new TenantContext(tenant));
            await store1.AddAsync(lifecycle);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCoreUserLifecycleStore(db2, new TenantContext(tenant));
            var existing = await store2.GetAsync(new UserLifecycleKey(tenant, userKey));
            var updated = existing!.ChangeStatus(DateTimeOffset.UtcNow, UserStatus.Suspended);
            await store2.SaveAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store3 = new EfCoreUserLifecycleStore(db3, new TenantContext(tenant));
            var result = await store3.GetAsync(new UserLifecycleKey(tenant, userKey));

            Assert.Equal(1, result!.Version);
            Assert.Equal(UserStatus.Suspended, result.Status);
        }
    }

    [Fact]
    public async Task Save_With_Wrong_Version_Should_Throw()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var lifecycle = UserLifecycle.Create(
            tenant,
            userKey,
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCoreUserLifecycleStore(db1, new TenantContext(tenant));
            await store1.AddAsync(lifecycle);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCoreUserLifecycleStore(db2, new TenantContext(tenant));
            var existing = await store2.GetAsync(new UserLifecycleKey(tenant, userKey));
            var updated = existing!.ChangeStatus(DateTimeOffset.UtcNow, UserStatus.Suspended);

            await Assert.ThrowsAsync<UAuthConcurrencyException>(() =>
                store2.SaveAsync(updated, expectedVersion: 999));
        }
    }

    [Fact]
    public async Task Should_Not_See_Data_From_Other_Tenant()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant1 = TenantKeys.Single;
        var tenant2 = TenantKey.FromInternal("tenant-2");

        var store1 = new EfCoreUserLifecycleStore(db, new TenantContext(tenant1));
        var store2 = new EfCoreUserLifecycleStore(db, new TenantContext(tenant2));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var lifecycle = UserLifecycle.Create(
            tenant1,
            userKey,
            DateTimeOffset.UtcNow);

        await store1.AddAsync(lifecycle);

        var result = await store2.GetAsync(new UserLifecycleKey(tenant2, userKey));

        Assert.Null(result);
    }

    [Fact]
    public async Task Soft_Delete_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserLifecycleStore(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var lifecycle = UserLifecycle.Create(
            tenant,
            userKey,
            DateTimeOffset.UtcNow);

        await store.AddAsync(lifecycle);

        await store.DeleteAsync(
            new UserLifecycleKey(tenant, userKey),
            expectedVersion: 0,
            DeleteMode.Soft,
            DateTimeOffset.UtcNow);

        var result = await store.GetAsync(new UserLifecycleKey(tenant, userKey));

        Assert.NotNull(result);
        Assert.NotNull(result!.DeletedAt);
    }

    [Fact]
    public async Task Delete_Should_Increment_SecurityVersion()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var lifecycle = UserLifecycle.Create(
            tenant,
            userKey,
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCoreUserLifecycleStore(db1, new TenantContext(tenant));
            await store1.AddAsync(lifecycle);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCoreUserLifecycleStore(db2, new TenantContext(tenant));

            var existing = await store2.GetAsync(new UserLifecycleKey(tenant, userKey));
            var deleted = existing!.MarkDeleted(DateTimeOffset.UtcNow);

            await store2.SaveAsync(deleted, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store3 = new EfCoreUserLifecycleStore(db3, new TenantContext(tenant));

            var result = await store3.GetAsync(new UserLifecycleKey(tenant, userKey));

            Assert.Equal(1, result!.SecurityVersion);
        }
    }
}
