using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public sealed class EfCoreUserProfileStoreContractTests
    : UserProfileStoreContractTests
{
    protected override async Task<IUserProfileStoreTestDatabase>
        CreateDatabaseAsync()
    {
        var database = new Database();
        await database.InitializeAsync();
        return database;
    }

    private sealed class Database : IUserProfileStoreTestDatabase
    {
        private readonly SqliteConnection _connection;
        private readonly UAuthUserDbContext _db;

        public Database()
        {
            _connection =
                new SqliteConnection("Data Source=:memory:");

            var options =
                new DbContextOptionsBuilder<UAuthUserDbContext>()
                    .UseSqlite(_connection)
                    .Options;

            _db = new UAuthUserDbContext(options);
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
            await _db.Database.EnsureCreatedAsync();
        }

        public IUserProfileStore CreateStore(TenantKey tenant)
        {
            return new EfCoreUserProfileStore<UAuthUserDbContext>(
                _db,
                new TenantExecutionContext(tenant));
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
