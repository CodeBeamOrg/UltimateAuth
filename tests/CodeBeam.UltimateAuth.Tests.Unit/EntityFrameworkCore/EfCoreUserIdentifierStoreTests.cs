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

public class EfCoreUserIdentifierStoreTests : EfCoreTestBase
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
        var store = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db, new TenantExecutionContext(tenant));
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var identifier = UserIdentifier.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            UserIdentifierType.Username,
            "user",
            "user",
            DateTimeOffset.UtcNow,
            isPrimary: true);

        await store.AddAsync(identifier);
        var result = await store.GetAsync(identifier.Id);

        Assert.NotNull(result);
        Assert.Equal(identifier.Id, result!.Id);
    }

    [Fact]
    public async Task Exists_Should_Return_True_When_Exists()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);
        var tenant = TenantKeys.Single;
        var store = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db, new TenantExecutionContext(tenant));
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var identifier = UserIdentifier.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            UserIdentifierType.Username,
            "user",
            "user",
            DateTimeOffset.UtcNow,
            isPrimary: true);

        await store.AddAsync(identifier);
        var exists = await store.ExistsAsync(identifier.Id);

        Assert.True(exists);
    }

    [Fact]
    public async Task Save_With_Wrong_Version_Should_Throw()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);
        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        await using var db1 = CreateDb(connection);
        var store1 = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db1, new TenantExecutionContext(tenant));

        var identifier = UserIdentifier.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            UserIdentifierType.Username,
            "user",
            "user",
            DateTimeOffset.UtcNow,
            isPrimary: true);

        await store1.AddAsync(identifier);

        await using var db2 = CreateDb(connection);
        var store2 = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db2, new TenantExecutionContext(tenant));

        var updated = identifier.SetPrimary(DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<UAuthConcurrencyException>(() =>
            store2.SaveAsync(updated, expectedVersion: 999));
    }

    [Fact]
    public async Task Save_Should_Increment_Version()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var identifier = UserIdentifier.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            UserIdentifierType.Username,
            "user",
            "user",
            DateTimeOffset.UtcNow,
            isPrimary: true);

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db1, new TenantExecutionContext(tenant));
            await store1.AddAsync(identifier);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db2, new TenantExecutionContext(tenant));
            var existing = await store2.GetAsync(identifier.Id);
            var updated = existing!.SetPrimary(DateTimeOffset.UtcNow);
            await store2.SaveAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store3 = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db3, new TenantExecutionContext(tenant));
            var result = await store3.GetAsync(identifier.Id);
            Assert.Equal(1, result!.Version);
        }
    }

    [Fact]
    public async Task Should_Not_See_Data_From_Other_Tenant()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);
        var tenant1 = TenantKeys.Single;
        var tenant2 = TenantKey.FromInternal("tenant-2");
        var store1 = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db, new TenantExecutionContext(tenant1));
        var store2 = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db, new TenantExecutionContext(tenant2));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var identifier = UserIdentifier.Create(
            Guid.NewGuid(),
            tenant1,
            userKey,
            UserIdentifierType.Username,
            "user",
            "user",
            DateTimeOffset.UtcNow,
            isPrimary: true);

        await store1.AddAsync(identifier);
        var result = await store2.GetAsync(identifier.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Soft_Delete_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);
        var tenant = TenantKeys.Single;
        var store = new EfCoreUserIdentifierStore<UAuthUserDbContext>(db, new TenantExecutionContext(tenant));
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var identifier = UserIdentifier.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            UserIdentifierType.Username,
            "user",
            "user",
            DateTimeOffset.UtcNow,
            isPrimary: true);

        await store.AddAsync(identifier);
        await store.DeleteAsync(identifier.Id, 0, DeleteMode.Soft, DateTimeOffset.UtcNow);
        var result = await store.GetAsync(identifier.Id);

        Assert.NotNull(result);
        Assert.NotNull(result!.DeletedAt);
    }

    [Fact]
    public void ChangeValue_Should_Update_Value()
    {
        var now = DateTimeOffset.UtcNow;

        var id = UserIdentifier.Create(
            Guid.NewGuid(),
            TenantKeys.Single,
            UserKey.FromGuid(Guid.NewGuid()),
            UserIdentifierType.Username,
            "user",
            "user",
            now);

        id.ChangeValue("new", "new", now);

        Assert.Equal("new", id.Value);
        Assert.Equal("new", id.NormalizedValue);
        Assert.Null(id.VerifiedAt);
    }

    [Fact]
    public void ChangeValue_SameValue_Should_Throw()
    {
        var now = DateTimeOffset.UtcNow;

        var id = UserIdentifier.Create(
            Guid.NewGuid(),
            TenantKeys.Single,
            UserKey.FromGuid(Guid.NewGuid()),
            UserIdentifierType.Username,
            "user",
            "user",
            now);

        Assert.Throws<UAuthIdentifierConflictException>(() => id.ChangeValue("user", "user", now));
    }

    [Fact]
    public void SetPrimary_AlreadyPrimary_Should_NotChange()
    {
        var now = DateTimeOffset.UtcNow;

        var id = UserIdentifier.Create(
            Guid.NewGuid(),
            TenantKeys.Single,
            UserKey.FromGuid(Guid.NewGuid()),
            UserIdentifierType.Username,
            "user",
            "user",
            now,
            isPrimary: true);

        var result = id.SetPrimary(now);

        Assert.Same(id, result);
    }

    [Fact]
    public void UnsetPrimary_Should_Work()
    {
        var now = DateTimeOffset.UtcNow;

        var id = UserIdentifier.Create(
            Guid.NewGuid(),
            TenantKeys.Single,
            UserKey.FromGuid(Guid.NewGuid()),
            UserIdentifierType.Username,
            "user",
            "user",
            now,
            isPrimary: true);

        id.UnsetPrimary(now);

        Assert.False(id.IsPrimary);
    }

    [Fact]
    public void UnsetPrimary_NotPrimary_Should_Throw()
    {
        var now = DateTimeOffset.UtcNow;

        var id = UserIdentifier.Create(
            Guid.NewGuid(),
            TenantKeys.Single,
            UserKey.FromGuid(Guid.NewGuid()),
            UserIdentifierType.Username,
            "user",
            "user",
            now);

        Assert.Throws<UAuthIdentifierConflictException>(() => id.UnsetPrimary(now));
    }

    [Fact]
    public void MarkVerified_Should_SetVerifiedAt()
    {
        var now = DateTimeOffset.UtcNow;

        var id = UserIdentifier.Create(
            Guid.NewGuid(),
            TenantKeys.Single,
            UserKey.FromGuid(Guid.NewGuid()),
            UserIdentifierType.Username,
            "user",
            "user",
            now);

        id.MarkVerified(now);

        Assert.True(id.IsVerified);
    }

    [Fact]
    public void Deleted_Entity_Should_Not_Allow_Mutation()
    {
        var now = DateTimeOffset.UtcNow;

        var id = UserIdentifier.Create(
            Guid.NewGuid(),
            TenantKeys.Single,
            UserKey.FromGuid(Guid.NewGuid()),
            UserIdentifierType.Username,
            "user",
            "user",
            now);

        id.MarkDeleted(now);

        Assert.Throws<UAuthIdentifierNotFoundException>(() =>
            id.SetPrimary(now));
    }
}
