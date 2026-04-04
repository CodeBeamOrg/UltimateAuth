using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Data.Sqlite;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EfCoreCredentialStoreTests : EfCoreTestBase
{
    private static UAuthCredentialDbContext CreateDb(SqliteConnection connection)
    {
        return CreateDbContext<UAuthCredentialDbContext>(connection, options => new UAuthCredentialDbContext(options));
    }

    [Fact]
    public async Task Add_And_Get_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            "hash",
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await store.AddAsync(credential);

        var result = await store.GetAsync(new CredentialKey(tenant, credential.Id));

        Assert.NotNull(result);
        Assert.Equal("hash", result!.SecretHash);
    }

    [Fact]
    public async Task Exists_Should_Return_True_When_Exists()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            "hash",
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await store.AddAsync(credential);
        var exists = await store.ExistsAsync(new CredentialKey(tenant, credential.Id));

        Assert.True(exists);
    }

    [Fact]
    public async Task Save_Should_Increment_Version()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            "hash",
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db1, new TenantContext(tenant));
            await store1.AddAsync(credential);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db2, new TenantContext(tenant));
            var existing = await store2.GetAsync(new CredentialKey(tenant, credential.Id));
            var updated = existing!.ChangeSecret("new_hash", DateTimeOffset.UtcNow);
            await store2.SaveAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store3 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db3, new TenantContext(tenant));
            var result = await store3.GetAsync(new CredentialKey(tenant, credential.Id));

            Assert.Equal(1, result!.Version);
            Assert.Equal("new_hash", result.SecretHash);
        }
    }

    [Fact]
    public async Task Save_With_Wrong_Version_Should_Throw()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            "hash",
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db1, new TenantContext(tenant));
            await store1.AddAsync(credential);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db2, new TenantContext(tenant));

            var existing = await store2.GetAsync(new CredentialKey(tenant, credential.Id));
            var updated = existing!.ChangeSecret("new_hash", DateTimeOffset.UtcNow);

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

        var store1 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db, new TenantContext(tenant1));
        var store2 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db, new TenantContext(tenant2));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant1,
            userKey,
            "hash",
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await store1.AddAsync(credential);
        var result = await store2.GetAsync(new CredentialKey(tenant2, credential.Id));

        Assert.Null(result);
    }

    [Fact]
    public async Task Soft_Delete_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            "hash",
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await store.AddAsync(credential);

        await store.DeleteAsync(
            new CredentialKey(tenant, credential.Id),
            expectedVersion: 0,
            DeleteMode.Soft,
            DateTimeOffset.UtcNow);

        var result = await store.GetAsync(new CredentialKey(tenant, credential.Id));

        Assert.NotNull(result);
        Assert.NotNull(result!.DeletedAt);
    }

    [Fact]
    public async Task Revoke_Should_Persist()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            "hash",
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db1, new TenantContext(tenant));
            await store1.AddAsync(credential);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db2, new TenantContext(tenant));
            var existing = await store2.GetAsync(new CredentialKey(tenant, credential.Id));
            var revoked = existing!.Revoke(DateTimeOffset.UtcNow);
            await store2.SaveAsync(revoked, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store3 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db3, new TenantContext(tenant));
            var result = await store3.GetAsync(new CredentialKey(tenant, credential.Id));

            Assert.True(result!.IsRevoked);
        }
    }

    [Fact]
    public async Task ChangeSecret_Should_Update_SecurityState()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            "hash",
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store1 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db1, new TenantContext(tenant));
            await store1.AddAsync(credential);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store2 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db2, new TenantContext(tenant));
            var existing = await store2.GetAsync(new CredentialKey(tenant, credential.Id));
            var updated = existing!.ChangeSecret("new_hash", DateTimeOffset.UtcNow);

            await store2.SaveAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store3 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db3, new TenantContext(tenant));
            var result = await store3.GetAsync(new CredentialKey(tenant, credential.Id));

            Assert.Equal("new_hash", result!.SecretHash);
            Assert.NotNull(result.UpdatedAt);
        }
    }
}
