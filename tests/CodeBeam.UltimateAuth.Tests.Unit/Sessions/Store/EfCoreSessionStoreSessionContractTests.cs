using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.Sessions.Contracts;

public sealed class EfCoreSessionStoreSessionContractTests
    : SessionStoreSessionContractTests
{
    protected override async Task<ISessionStoreTestDatabase>
        CreateDatabaseAsync()
    {
        var database = new Database();
        await database.InitializeAsync();
        return database;
    }

    private sealed class Database
        : ISessionStoreTestDatabase
    {
        private readonly SqliteConnection _connection;
        private readonly UAuthSessionDbContext _db;

        public Database()
        {
            _connection = new SqliteConnection(
                "Data Source=:memory:");

            var options =
                new DbContextOptionsBuilder<UAuthSessionDbContext>()
                    .UseSqlite(_connection)
                    .Options;

            _db = new UAuthSessionDbContext(options);
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
            await _db.Database.EnsureCreatedAsync();
        }

        public ISessionStore CreateStore(TenantKey tenant)
        {
            return new EfCoreSessionStore<UAuthSessionDbContext>(
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

