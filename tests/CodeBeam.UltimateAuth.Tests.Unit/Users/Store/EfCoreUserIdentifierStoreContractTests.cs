using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class EfCoreUserIdentifierStoreContractTests
    : UserIdentifierStoreContractTests
{
    protected override async Task<IUserIdentifierStoreTestDatabase>
        CreateDatabaseAsync()
    {
        var db = new Database();
        await db.InitializeAsync();
        return db;
    }

    private sealed class Database : IUserIdentifierStoreTestDatabase
    {
        private readonly SqliteConnection _connection;
        private readonly UAuthUserDbContext _db;

        public Database()
        {
            _connection = new SqliteConnection("Data Source=:memory:");

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

        public IUserIdentifierStore CreateStore(TenantKey tenant)
        {
            return new EfCoreUserIdentifierStore<UAuthUserDbContext>(
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
