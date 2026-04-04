using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

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
        var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantContext(tenant));
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
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantContext(tenant));

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
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantContext(tenant));

            await store.ExecuteAsync(async ct =>
            {
                await store.RevokeAsync(tokenHash, DateTimeOffset.UtcNow, null, ct);
            });
        }

        await using (var db = CreateDb(connection))
        {
            var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantContext(tenant));
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
        var store = new EfCoreRefreshTokenStore<UAuthTokenDbContext>(db, new TenantContext(tenant));
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
}
