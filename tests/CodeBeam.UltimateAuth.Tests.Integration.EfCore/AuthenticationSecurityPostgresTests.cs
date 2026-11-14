using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CodeBeam.UltimateAuth.Tests.Integration.EfCore.Authentication;

public sealed class AuthenticationSecurityPostgresTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Account_state_must_be_unique_when_credential_type_is_null()
    {
        await using var database = await postgres.CreateDatabaseAsync();

        var options =
            new DbContextOptionsBuilder<UAuthAuthenticationDbContext>()
                .UseNpgsql(database.ConnectionString)
                .Options;

        await using (var db = new UAuthAuthenticationDbContext(options))
        {
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        await using var connection =
            new NpgsqlConnection(database.ConnectionString);

        await connection.OpenAsync(TestContext.Current.CancellationToken);

        const string insert = """
            INSERT INTO "UAuth_Authentication"
                ("Id", "Tenant", "UserKey", "Scope",
                 "CredentialType", "FailedAttempts",
                 "RequiresReauthentication", "ResetAttempts",
                 "SecurityVersion")
            VALUES
                (@id, 'default', 'test-user', 0,
                 NULL, 0, FALSE, 0, 0);
            """;

        async Task InsertAsync()
        {
            await using var command = new NpgsqlCommand(insert, connection);
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            await command.ExecuteNonQueryAsync();
        }

        await InsertAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(
            InsertAsync);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
    }

    [Fact]
    public async Task Concurrent_account_creation_should_persist_one_state()
    {
        await using var database = await postgres.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            database.ConnectionString);

        var tenant = TenantKey.FromExternal("concurrency-test");
        var user = UserKey.FromString("concurrent-user");

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var store1 = CreateStore(scope1.ServiceProvider, tenant);
        var store2 = CreateStore(scope2.ServiceProvider, tenant);

        var ct = TestContext.Current.CancellationToken;

        // Both requests observe the state before creation.
        Assert.Null(await store1.GetAsync(
            user, AuthenticationSecurityScope.Account, null, ct));

        Assert.Null(await store2.GetAsync(
            user, AuthenticationSecurityScope.Account, null, ct));

        var state1 = AuthenticationSecurityState.CreateAccount(tenant, user);
        var state2 = AuthenticationSecurityState.CreateAccount(tenant, user);

        var outcomes = await Task.WhenAll(
            AttemptAddAsync(store1, state1, ct),
            AttemptAddAsync(store2, state2, ct));

        Assert.Equal(1, outcomes.Count(x => x));
        Assert.Equal(1, outcomes.Count(x => !x));

        await using var verification = provider.CreateAsyncScope();
        var stored = await CreateStore(
            verification.ServiceProvider, tenant)
            .GetAsync(user, AuthenticationSecurityScope.Account, null, ct);

        Assert.NotNull(stored);
    }

    [Fact]
    public async Task Stale_security_version_should_be_rejected()
    {
        await using var database = await postgres.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            database.ConnectionString);

        var tenant = TenantKey.FromExternal("version-test");
        var user = UserKey.FromString("version-user");
        var ct = TestContext.Current.CancellationToken;

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var store1 = CreateStore(scope1.ServiceProvider, tenant);
        var store2 = CreateStore(scope2.ServiceProvider, tenant);

        await store1.AddAsync(
            AuthenticationSecurityState.CreateAccount(tenant, user), ct);

        var original1 = await store1.GetAsync(
            user, AuthenticationSecurityScope.Account, null, ct);

        var original2 = await store2.GetAsync(
            user, AuthenticationSecurityScope.Account, null, ct);

        Assert.NotNull(original1);
        Assert.NotNull(original2);

        var now = DateTimeOffset.UtcNow;

        var first = original1!.RegisterFailure(
            now, 10, TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(15));

        await store1.UpdateAsync(
            first, original1.SecurityVersion, ct);

        var second = original2!.RegisterFailure(
            now, 10, TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(15));

        await Assert.ThrowsAsync<UAuthConflictException>(
            () => store2.UpdateAsync(
                second, original2.SecurityVersion, ct));

        await using var verification = provider.CreateAsyncScope();

        var persisted = await CreateStore(
            verification.ServiceProvider, tenant)
            .GetAsync(user, AuthenticationSecurityScope.Account, null, ct);

        Assert.NotNull(persisted);
        Assert.Equal(1, persisted!.FailedAttempts);
        Assert.Equal(first.SecurityVersion, persisted.SecurityVersion);
    }

    private static async Task<bool> AttemptAddAsync(
        IAuthenticationSecurityStateStore store,
        AuthenticationSecurityState state,
        CancellationToken ct)
    {
        try
        {
            await store.AddAsync(state, ct);
            return true;
        }
        catch (UAuthConflictException)
        {
            return false;
        }
    }


    private static async Task<ServiceProvider> CreateProviderAsync(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddUltimateAuthAuthenticationEntityFrameworkCore<UAuthAuthenticationDbContext>(options => options.UseNpgsql(connectionString));

        var provider = services.BuildServiceProvider(validateScopes: true);

        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<UAuthAuthenticationDbContext>();

        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        return provider;
    }

    private static IAuthenticationSecurityStateStore CreateStore(IServiceProvider services, TenantKey tenant)
    {
        return services.GetRequiredService<IAuthenticationSecurityStateStoreFactory>().Create(tenant);
    }
}
