using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.Contracts;
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
            ProfileKey.Default,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await store.AddAsync(profile);
        var result = await store.GetAsync(new UserProfileKey(tenant, userKey, ProfileKey.Default));

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
            ProfileKey.Default,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await store.AddAsync(profile);
        var exists = await store.ExistsAsync(new UserProfileKey(tenant, userKey, ProfileKey.Default));

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
            ProfileKey.Default,
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
            var existing = await store2.GetAsync(new UserProfileKey(tenant, userKey, ProfileKey.Default));
            var updated = existing!.UpdateName(existing.FirstName, existing.LastName, "new", DateTimeOffset.UtcNow);
            await store2.SaveAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store3 = new EfCoreUserProfileStore<UAuthUserDbContext>(db3, new TenantContext(tenant));
            var result = await store3.GetAsync(new UserProfileKey(tenant, userKey, ProfileKey.Default));

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
            ProfileKey.Default,
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
            var existing = await store2.GetAsync(new UserProfileKey(tenant, userKey, ProfileKey.Default));
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
            ProfileKey.Default,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await store1.AddAsync(profile);
        var result = await store2.GetAsync(new UserProfileKey(tenant2, userKey, ProfileKey.Default));

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
            ProfileKey.Default,
            DateTimeOffset.UtcNow,
            displayName: "display",
            firstName: "first",
            lastName: "last"
            );

        await store.AddAsync(profile);

        await store.DeleteAsync(
            new UserProfileKey(tenant, userKey, ProfileKey.Default),
            expectedVersion: 0,
            DeleteMode.Soft,
            DateTimeOffset.UtcNow);

        var result = await store.GetAsync(new UserProfileKey(tenant, userKey, ProfileKey.Default));

        Assert.NotNull(result);
        Assert.NotNull(result!.DeletedAt);
    }

    [Fact]
    public async Task Same_User_Can_Have_Multiple_Profiles()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var defaultProfile = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Default,
            DateTimeOffset.UtcNow,
            displayName: "default");

        var businessProfile = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Parse("business", null),
            DateTimeOffset.UtcNow,
            displayName: "business");

        await store.AddAsync(defaultProfile);
        await store.AddAsync(businessProfile);

        var p1 = await store.GetAsync(new UserProfileKey(tenant, userKey, ProfileKey.Default));
        var p2 = await store.GetAsync(new UserProfileKey(tenant, userKey, ProfileKey.Parse("business", null)));

        Assert.NotNull(p1);
        Assert.NotNull(p2);
        Assert.NotEqual(p1!.ProfileKey, p2!.ProfileKey);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Correct_Profile_By_ProfileKey()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        await store.AddAsync(UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Default,
            DateTimeOffset.UtcNow,
            displayName: "default"));

        await store.AddAsync(UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Parse("business", null),
            DateTimeOffset.UtcNow,
            displayName: "business"));

        var result = await store.GetAsync(
            new UserProfileKey(tenant, userKey, ProfileKey.Parse("business", null)));

        Assert.Equal("business", result!.DisplayName);
    }

    [Fact]
    public async Task GetByUsersAsync_Should_Filter_By_ProfileKey()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        await store.AddAsync(UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Default,
            DateTimeOffset.UtcNow,
            displayName: "default"));

        await store.AddAsync(UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Parse("business", null),
            DateTimeOffset.UtcNow,
            displayName: "business"));

        var results = await store.GetByUsersAsync(
            new[] { userKey },
            ProfileKey.Default);

        Assert.Single(results);
        Assert.Equal(ProfileKey.Default, results[0].ProfileKey);
    }

    [Fact]
    public async Task Should_Not_Allow_Duplicate_ProfileKey_For_Same_User()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var profile1 = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Default,
            DateTimeOffset.UtcNow);

        var profile2 = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Default,
            DateTimeOffset.UtcNow);

        await store.AddAsync(profile1);

        await Assert.ThrowsAsync<UAuthConflictException>(() =>
            store.AddAsync(profile2));
    }

    [Fact]
    public async Task Delete_Should_Not_Affect_Other_Profiles()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreUserProfileStore<UAuthUserDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var defaultProfile = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Default,
            DateTimeOffset.UtcNow);

        var businessProfile = UserProfile.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            ProfileKey.Parse("business", null),
            DateTimeOffset.UtcNow);

        await store.AddAsync(defaultProfile);
        await store.AddAsync(businessProfile);

        await store.DeleteAsync(
            new UserProfileKey(tenant, userKey, ProfileKey.Default),
            0,
            DeleteMode.Soft,
            DateTimeOffset.UtcNow);

        var defaultResult = await store.GetAsync(
            new UserProfileKey(tenant, userKey, ProfileKey.Default));

        var businessResult = await store.GetAsync(
            new UserProfileKey(tenant, userKey, ProfileKey.Parse("business", null)));

        Assert.NotNull(defaultResult!.DeletedAt);
        Assert.Null(businessResult!.DeletedAt);
    }
}
