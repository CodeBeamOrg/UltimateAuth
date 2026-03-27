using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Data.Sqlite;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EfCoreAuthenticationStoreTests : EfCoreTestBase
{
    private static UAuthAuthenticationDbContext CreateDb(SqliteConnection connection)
    {
        return CreateDbContext<UAuthAuthenticationDbContext>(connection, options => new UAuthAuthenticationDbContext(options));
    }

    [Fact]
    public async Task Add_And_Get_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var state = AuthenticationSecurityState.CreateAccount(
            tenant,
            userKey);

        await store.AddAsync(state);

        var result = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, null);

        Assert.NotNull(result);
        Assert.Equal(state.Id, result!.Id);
    }

    [Fact]
    public async Task Update_With_RegisterFailure_Should_Increment_Version()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var state = AuthenticationSecurityState.CreateAccount(tenant, userKey);

        await using (var db1 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db1, new TenantContext(tenant));
            await store.AddAsync(state);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db2, new TenantContext(tenant));
            var existing = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, null);

            var updated = existing!.RegisterFailure(
                DateTimeOffset.UtcNow,
                threshold: 3,
                lockoutDuration: TimeSpan.FromMinutes(5));

            await store.UpdateAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db3, new TenantContext(tenant));
            var result = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, null);

            Assert.Equal(1, result!.SecurityVersion);
            Assert.Equal(1, result.FailedAttempts);
        }
    }

    [Fact]
    public async Task Update_With_Wrong_Version_Should_Throw()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var state = AuthenticationSecurityState.CreateAccount(tenant, userKey);
        await store.AddAsync(state);
        var updated = state.RegisterFailure(DateTimeOffset.UtcNow, 3, TimeSpan.FromMinutes(5));

        await Assert.ThrowsAsync<UAuthConflictException>(() => store.UpdateAsync(updated, expectedVersion: 999));
    }

    [Fact]
    public async Task RegisterSuccess_Should_Clear_Failures()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var state = AuthenticationSecurityState.CreateAccount(tenant, userKey)
            .RegisterFailure(DateTimeOffset.UtcNow, 3, TimeSpan.FromMinutes(5));

        await using (var db1 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db1, new TenantContext(tenant));
            await store.AddAsync(state);
        }

        await using (var db2 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db2, new TenantContext(tenant));
            var existing = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, null);
            var updated = existing!.RegisterSuccess();
            await store.UpdateAsync(updated, expectedVersion: 1);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db3, new TenantContext(tenant));
            var result = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, null);

            Assert.Equal(0, result!.FailedAttempts);
            Assert.Null(result.LockedUntil);
        }
    }

    [Fact]
    public async Task BeginReset_And_Consume_Should_Work()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var state = AuthenticationSecurityState.CreateAccount(tenant, userKey);

        await using (var db1 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db1, new TenantContext(tenant));
            await store.AddAsync(state);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using (var db2 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db2, new TenantContext(tenant));
            var existing = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, null);
            var updated = existing!.BeginReset("hash", now, TimeSpan.FromMinutes(10));
            await store.UpdateAsync(updated, expectedVersion: 0);
        }

        await using (var db3 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db3, new TenantContext(tenant));
            var existing = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, null);
            var consumed = existing!.ConsumeReset(DateTimeOffset.UtcNow);
            await store.UpdateAsync(consumed, expectedVersion: 1);
        }

        await using (var db4 = CreateDb(connection))
        {
            var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db4, new TenantContext(tenant));
            var result = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, null);
            Assert.NotNull(result!.ResetConsumedAt);
        }
    }

    [Fact]
    public async Task Delete_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db, new TenantContext(tenant));

        var userKey = UserKey.FromGuid(Guid.NewGuid());

        var state = AuthenticationSecurityState.CreateAccount(tenant, userKey);

        await store.AddAsync(state);

        await store.DeleteAsync(userKey, AuthenticationSecurityScope.Account, null);

        var result = await store.GetAsync(userKey, AuthenticationSecurityScope.Account, null);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Not_See_Data_From_Other_Tenant()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant1 = TenantKeys.Single;
        var tenant2 = TenantKey.FromInternal("tenant-2");

        var store1 = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db, new TenantContext(tenant1));
        var store2 = new EfCoreAuthenticationSecurityStateStore<UAuthAuthenticationDbContext>(db, new TenantContext(tenant2));

        var userKey = UserKey.FromGuid(Guid.NewGuid());
        var state = AuthenticationSecurityState.CreateAccount(tenant1, userKey);
        await store1.AddAsync(state);
        var result = await store2.GetAsync(userKey, AuthenticationSecurityScope.Account, null);

        Assert.Null(result);
    }
}
