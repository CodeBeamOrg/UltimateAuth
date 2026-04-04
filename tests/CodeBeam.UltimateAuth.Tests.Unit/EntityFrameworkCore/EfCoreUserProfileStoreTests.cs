using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Data.Sqlite;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EfCoreUserProfileStoreTests : EfCoreTestBase
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
        var store = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var profile = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await store.AddAsync(profile);
        var result = await store.GetAsync(new UserProfileKey(tenant, userKey));

        Assert.NotNull(result);
        Assert.Equal(userKey, result!.UserKey);
    }

    [Fact]
    public async Task Exists_Should_Return_True_When_Exists()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var profile = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await store.AddAsync(profile);
        var exists = await store.ExistsAsync(new UserProfileKey(tenant, userKey));

        Assert.True(exists);
    }

    [Fact]
    public async Task Save_Should_Increment_Version()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var profile = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCoreUserProfileStore<UAuthUserDbContext>(db1, new TenantContext(tenant));
            await store1.AddAsync(profile);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCoreUserProfileStore<UAuthUserDbContext>(db2, new TenantContext(tenant));
            var existing = await store2.GetAsync(new UserProfileKey(tenant, userKey));
            var updated = existing!.UpdateName(existing.FirstName, existing.LastName, "new", DateTimeOffset.UtcNow);
            await store2.SaveAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store3 = new EfCoreUserProfileStore<UAuthUserDbContext>(db3, new TenantContext(tenant));
            var result = await store3.GetAsync(new UserProfileKey(tenant, userKey));

            Assert.Equal(1, result!.Version);
            Assert.Equal("new", result.DisplayName);
        }
    }

    [Fact]
    public async Task Save_With_Wrong_Version_Should_Throw()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var profile = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCoreUserProfileStore<UAuthUserDbContext>(db1, new TenantContext(tenant));
            await store1.AddAsync(profile);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCoreUserProfileStore<UAuthUserDbContext>(db2, new TenantContext(tenant));
            var existing = await store2.GetAsync(new UserProfileKey(tenant, userKey));
            var updated = existing!.UpdateName(existing.FirstName, existing.LastName, "new", DateTimeOffset.UtcNow);

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

        var store1 = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant1));
        var store2 = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant2));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var profile = UserProfile.Create(
            Guid.NewGuid(),
            tenant1,
            userKey,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await store1.AddAsync(profile);
        var result = await store2.GetAsync(new UserProfileKey(tenant2, userKey));

        Assert.Null(result);
    }

    [Fact]
    public async Task Soft_Delete_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var profile = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await store.AddAsync(profile);

        await store.DeleteAsync(
            new UserProfileKey(tenant, userKey),
            expectedVersion: 0,
            DeleteMode.Soft,
            DateTimeOffset.UtcNow);

        var result = await store.GetAsync(new UserProfileKey(tenant, userKey));

        Assert.NotNull(result);
        Assert.NotNull(result!.DeletedAt);
    }
}
