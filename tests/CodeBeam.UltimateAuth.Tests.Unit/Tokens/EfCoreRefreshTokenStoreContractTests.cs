using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.Tokens.Contracts;

public sealed class EfCoreRefreshTokenStoreContractTests
    : RefreshTokenStoreContractTests
{
    protected override async Task<IRefreshTokenStoreTestDatabase>
        CreateDatabaseAsync()
    {
        var db = new Database();
        await db.InitializeAsync();
        return db;
    }

    private sealed class Database
        : IRefreshTokenStoreTestDatabase
    {
        private readonly SqliteConnection _connection;
        private readonly UAuthTokenDbContext _db;

        public Database()
        {
            _connection =
                new SqliteConnection("Data Source=:memory:");

            var options =
                new DbContextOptionsBuilder<UAuthTokenDbContext>()
                    .UseSqlite(_connection)
                    .Options;

            _db = new UAuthTokenDbContext(options);
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
            await _db.Database.EnsureCreatedAsync();
        }

        public IRefreshTokenStore CreateStore(TenantKey tenant)
            => new EfCoreRefreshTokenStore<UAuthTokenDbContext>(
                _db,
                new TenantExecutionContext(tenant));

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
