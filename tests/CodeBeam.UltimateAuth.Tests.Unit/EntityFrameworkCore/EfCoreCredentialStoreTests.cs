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
        var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(
            db,
            new TenantExecutionContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var hasher = new TestPasswordHasher();
        var passwordHash = hasher.Hash("123456");

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            passwordHash,
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await store.AddAsync(credential);

        var result = await store.GetAsync(new CredentialKey(tenant, credential.Id));

        Assert.NotNull(result);

        Assert.True(hasher.Verify(result.SecretHash, "123456"));
    }

    [Fact]
    public async Task Exists_Should_Return_True_When_Exists()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(
            db, new TenantExecutionContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var hasher = new TestPasswordHasher();
        var hash = hasher.Hash("123");

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            hash,
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await store.AddAsync(credential);

        var exists = await store.ExistsAsync(new CredentialKey(tenant, credential.Id));

        Assert.True(exists);
    }

    [Fact]
    public async Task Save_Should_Increment_Version_And_Update_Hash()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var hasher = new TestPasswordHasher();

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            hasher.Hash("old"),
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db1, new TenantExecutionContext(tenant));
            await store.AddAsync(credential);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db2, new TenantExecutionContext(tenant));

            var existing = await store.GetAsync(new CredentialKey(tenant, credential.Id));

            var updated = existing!.ChangeSecret(hasher.Hash("new"), DateTimeOffset.UtcNow);

            await store.SaveAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db3, new TenantExecutionContext(tenant));

            var result = await store.GetAsync(new CredentialKey(tenant, credential.Id));

            Assert.Equal(1, result!.Version);
            Assert.True(hasher.Verify(result.SecretHash, "new"));
        }
    }

    [Fact]
    public async Task Save_With_Wrong_Version_Should_Throw()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var hasher = new TestPasswordHasher();

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            hasher.Hash("123"),
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db1, new TenantExecutionContext(tenant));
            await store.AddAsync(credential);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db2, new TenantExecutionContext(tenant));

            var existing = await store.GetAsync(new CredentialKey(tenant, credential.Id));
            var updated = existing!.ChangeSecret(hasher.Hash("new"), DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<UAuthConcurrencyException>(() =>
                store.SaveAsync(updated, expectedVersion: 999));
        }
    }

    [Fact]
    public async Task Should_Not_See_Data_From_Other_Tenant()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant1 = TenantKeys.Single;
        var tenant2 = TenantKey.FromInternal("tenant-2");

        var hasher = new TestPasswordHasher();

        var store1 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db, new TenantExecutionContext(tenant1));
        var store2 = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db, new TenantExecutionContext(tenant2));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant1,
            userKey,
            hasher.Hash("123"),
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
        var hasher = new TestPasswordHasher();

        var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db, new TenantExecutionContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            hasher.Hash("123"),
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
        var hasher = new TestPasswordHasher();

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            hasher.Hash("123"),
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db1, new TenantExecutionContext(tenant));
            await store.AddAsync(credential);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db2, new TenantExecutionContext(tenant));

            var existing = await store.GetAsync(new CredentialKey(tenant, credential.Id));
            var revoked = existing!.Revoke(DateTimeOffset.UtcNow);

            await store.SaveAsync(revoked, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db3, new TenantExecutionContext(tenant));

            var result = await store.GetAsync(new CredentialKey(tenant, credential.Id));

            Assert.True(result!.IsRevoked);
        }
    }

    [Fact]
    public async Task ChangeSecret_Should_Update_SecurityState()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var hasher = new TestPasswordHasher();

        var credential = PasswordCredential.Create(
            Guid.NewGuid(),
            tenant,
            userKey,
            hasher.Hash("old"),
            CredentialSecurityState.Active(),
            new CredentialMetadata(),
            DateTimeOffset.UtcNow);

        await using (var db1 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db1, new TenantExecutionContext(tenant));
            await store.AddAsync(credential);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db2, new TenantExecutionContext(tenant));

            var existing = await store.GetAsync(new CredentialKey(tenant, credential.Id));
            var updated = existing!.ChangeSecret(hasher.Hash("new"), DateTimeOffset.UtcNow);

            await store.SaveAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store = new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(db3, new TenantExecutionContext(tenant));

            var result = await store.GetAsync(new CredentialKey(tenant, credential.Id));

            Assert.True(hasher.Verify(result!.SecretHash, "new"));
            Assert.NotNull(result.UpdatedAt);
        }
    }
}
