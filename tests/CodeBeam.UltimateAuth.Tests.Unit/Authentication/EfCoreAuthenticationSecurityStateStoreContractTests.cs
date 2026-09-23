using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.Authentication.Contracts;

public sealed class EfCoreAuthenticationSecurityStateStoreContractTests : AuthenticationSecurityStateStoreContractTests
{
    protected override async Task<IAuthenticationSecurityStateStoreTestDatabase>
        CreateDatabaseAsync()
    {
        var database = new Database();

        await database.InitializeAsync();

        return database;
    }

    private sealed class Database
        : IAuthenticationSecurityStateStoreTestDatabase
    {
        private readonly SqliteConnection _connection;

        private readonly DbContextOptions<UAuthAuthenticationDbContext>
            _options;

        private readonly List<UAuthAuthenticationDbContext>
            _contexts = [];

        public Database()
        {
            _connection =
                new SqliteConnection("Data Source=:memory:");

            _options =
                new DbContextOptionsBuilder<UAuthAuthenticationDbContext>()
                    .UseSqlite(_connection)
                    .Options;
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();

            await using var db =
                new UAuthAuthenticationDbContext(_options);

            await db.Database.EnsureCreatedAsync();
        }

        public IAuthenticationSecurityStateStore CreateStore(
            TenantKey tenant)
        {
            var db =
                new UAuthAuthenticationDbContext(_options);

            _contexts.Add(db);

            return new EfCoreAuthenticationSecurityStateStore<
                UAuthAuthenticationDbContext>(
                    db,
                    new TenantExecutionContext(tenant));
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var context in _contexts)
                await context.DisposeAsync();

            await _connection.DisposeAsync();
        }
    }

    // Aynı CreateState / MutateState / AssertMutationPersisted
    // implementation'ı.
}