using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials.Contracts;

public sealed class EfCorePasswordCredentialStoreContractTests
    : PasswordCredentialStoreContractTests
{
    protected override async Task<IPasswordCredentialStoreTestDatabase>
        CreateDatabaseAsync()
    {
        var database = new Database();
        await database.InitializeAsync();
        return database;
    }

    private sealed class Database
        : IPasswordCredentialStoreTestDatabase
    {
        private readonly SqliteConnection _connection;
        private readonly UAuthCredentialDbContext _db;

        public Database()
        {
            _connection =
                new SqliteConnection("Data Source=:memory:");

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

        public IPasswordCredentialStore CreateStore(
            TenantKey tenant)
        {
            return new EfCorePasswordCredentialStore<UAuthCredentialDbContext>(
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
