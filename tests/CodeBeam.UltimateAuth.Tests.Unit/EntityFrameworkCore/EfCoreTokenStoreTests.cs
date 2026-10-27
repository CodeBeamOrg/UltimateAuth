using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EfCoreTokenStoreTests : EfCoreTestBase
{
    private const string ValidRaw = "session-aaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static UAuthTokenDbContext CreateDb(SqliteConnection connection)
    {
        return CreateDbContext<UAuthTokenDbContext>(connection, options => new UAuthTokenDbContext(options));
    }

    [Fact]
    public async Task Store_And_Find_Should_Work()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantExecutionContext(tenant));
        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        var token = RefreshToken.Create(
            TokenId.From(Guid.NewGuid()),
            "hash",
            tenant,
            UserKey.FromGuid(Guid.NewGuid()),
            sessionId,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1)
            );

        await store.ExecuteAsync(async ct =>
        {
            await store.StoreAsync(token, ct);
        });

        var result = await store.FindByHashAsync("hash");

        Assert.NotNull(result);
        Assert.Equal("hash", result!.TokenHash);
    }

    [Fact]
    public async Task Revoke_Should_Set_RevokedAt()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        var tokenHash = "hash";
        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantExecutionContext(tenant));

            var token = RefreshToken.Create(
                TokenId.From(Guid.NewGuid()),
                "hash",
                tenant,
                UserKey.FromGuid(Guid.NewGuid()),
                sessionId,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1)
                );

            await store.ExecuteAsync(async ct =>
            {
                await store.StoreAsync(token, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantExecutionContext(tenant));

            await store.ExecuteAsync(async ct =>
            {
                await store.RevokeAsync(tokenHash, DateTimeOffset.UtcNow, null, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantExecutionContext(tenant));
            var result = await store.FindByHashAsync(tokenHash);

            Assert.NotNull(result!.RevokedAt);
        }
    }

    [Fact]
    public async Task Store_Outside_Transaction_Should_Throw()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantExecutionContext(tenant));
        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        var token = RefreshToken.Create(
                TokenId.From(Guid.NewGuid()),
                "hash",
                tenant,
                UserKey.FromGuid(Guid.NewGuid()),
                sessionId,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1)
                );

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.StoreAsync(token));
    }

    [Fact]
    public async Task ExecuteAsync_InsideAtomicExecutor_ShouldJoinTransaction()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        const string hash = "atomic-token-commit";

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(
                db,
                new TenantExecutionContext(tenant));

            await ExecuteAtomicallyAsync(db, async executor =>
            {
                await executor.ExecuteAsync(async ct =>
                {
                    Assert.NotNull(db.Database.CurrentTransaction);

                    await store.ExecuteAsync(async tokenCt =>
                    {
                        await store.StoreAsync(
                            CreateToken(hash, tenant),
                            tokenCt);
                    }, ct);

                    Assert.NotNull(db.Database.CurrentTransaction);
                });
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(
                db,
                new TenantExecutionContext(tenant));

            var persisted = await store.FindByHashAsync(hash);

            Assert.NotNull(persisted);
        }
    }

    [Fact]
    public async Task ExecuteAsync_InsideAtomicExecutor_ShouldRollbackWithOuterTransaction()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        const string hash = "atomic-token-rollback";

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(
                db,
                new TenantExecutionContext(tenant));

            await ExecuteAtomicallyAsync(db, async executor =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => executor.ExecuteAsync(async ct =>
                    {
                        await store.ExecuteAsync(async tokenCt =>
                        {
                            await store.StoreAsync(
                                CreateToken(hash, tenant),
                                tokenCt);
                        }, ct);

                        throw new InvalidOperationException(
                            "Simulated outer transaction failure");
                    }));
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(
                db,
                new TenantExecutionContext(tenant));

            var persisted = await store.FindByHashAsync(hash);

            Assert.Null(persisted);
        }
    }

    [Fact]
    public async Task ExecuteAsync_Generic_InsideAtomicExecutor_ShouldJoinTransaction()
    {
        using var connection = CreateOpenConnection();

        var tenant = TenantKeys.Single;
        const string hash = "atomic-token-generic";

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(
                db,
                new TenantExecutionContext(tenant));

            await ExecuteAtomicallyAsync(db, async executor =>
            {
                await executor.ExecuteAsync(async ct =>
                {
                    var result = await store.ExecuteAsync(async tokenCt =>
                    {
                        await store.StoreAsync(
                            CreateToken(hash, tenant),
                            tokenCt);

                        return 42;
                    }, ct);

                    Assert.Equal(42, result);
                });
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(
                db,
                new TenantExecutionContext(tenant));

            Assert.NotNull(await store.FindByHashAsync(hash));
        }
    }

    [Fact]
    public async Task ExecuteAsync_AfterFailure_ShouldResetExecutionState()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(
            db,
            new TenantExecutionContext(tenant));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.ExecuteAsync(_ =>
                throw new InvalidOperationException("Simulated failure")));

        await store.ExecuteAsync(async ct =>
        {
            await store.StoreAsync(
                CreateToken("after-failure", tenant),
                ct);
        });

        Assert.NotNull(
            await store.FindByHashAsync("after-failure"));
    }

    [Fact]
    public async Task ExecuteAsync_NestedOnSameStore_ShouldThrow()
    {
        using var connection = CreateOpenConnection();
        await using var db = CreateDb(connection);

        var tenant = TenantKeys.Single;
        var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(
            db,
            new TenantExecutionContext(tenant));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.ExecuteAsync(async ct =>
            {
                await store.ExecuteAsync(
                    _ => Task.CompletedTask,
                    ct);
            }));

        // The failed nested call must not leave the store locked.
        await store.ExecuteAsync(async ct =>
        {
            await store.StoreAsync(
                CreateToken("after-nested-failure", tenant),
                ct);
        });

        Assert.NotNull(
            await store.FindByHashAsync("after-nested-failure"));
    }

    private static RefreshToken CreateToken(string hash, TenantKey tenant)
    {
        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        var now = DateTimeOffset.UtcNow;

        return RefreshToken.Create(
            TokenId.From(Guid.NewGuid()),
            hash,
            tenant,
            UserKey.FromGuid(Guid.NewGuid()),
            sessionId,
            null,
            now,
            now.AddHours(1));
    }

    private static async Task ExecuteAtomicallyAsync(UAuthTokenDbContext db, Func<IUAuthAtomicExecutor, Task> action)
    {
        var services = new ServiceCollection();

        services.AddSingleton(db);

        CodeBeam.UltimateAuth.EntityFrameworkCore.Extensions
            .ServiceCollectionExtensions
            .AddUltimateAuthEntityFrameworkCore<UAuthTokenDbContext>(services);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var executor = scope.ServiceProvider
            .GetRequiredService<IUAuthAtomicExecutor>();

        await action(executor);
    }
}
