using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials.Contracts;

public sealed class EfCorePasswordHistoryStoreContractTests
    : PasswordHistoryStoreContractTests
{
    protected override async Task<IPasswordHistoryStoreTestDatabase>
        CreateDatabaseAsync()
    {
        var database = new Database();
        await database.InitializeAsync();
        return database;
    }

    private sealed class Database
        : IPasswordHistoryStoreTestDatabase
    {
        private readonly SqliteConnection _connection;
        private readonly UAuthCredentialDbContext _db;

        public Database()
        {
            _connection = new SqliteConnection(
                "Data Source=:memory:");

            var options =
                new DbContextOptionsBuilder<UAuthCredentialDbContext>()
                    .UseSqlite(_connection)
                    .Options;

            _db = new UAuthCredentialDbContext(options);
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
            await _db.Database.EnsureCreatedAsync();
        }

        public IPasswordHistoryStore CreateStore(TenantKey tenant)
            => new EfCorePasswordHistoryStore<UAuthCredentialDbContext>(
                _db, tenant);

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
