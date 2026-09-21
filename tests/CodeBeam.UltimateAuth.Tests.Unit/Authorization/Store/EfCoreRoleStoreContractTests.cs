using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Contracts.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.Authorization.Contracts;

public sealed class EfCoreRoleStoreContractTests : RoleStoreContractTests
{
    protected override async Task<IRoleStoreTestDatabase> CreateDatabaseAsync()
    {
        var database = new EfRoleStoreTestDatabase();
        await database.InitializeAsync();

        return database;
    }

    private sealed class EfRoleStoreTestDatabase : IRoleStoreTestDatabase
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<UAuthAuthorizationDbContext> _options;
        private readonly List<UAuthAuthorizationDbContext> _contexts = [];

        public EfRoleStoreTestDatabase()
        {
            _connection = new SqliteConnection("Data Source=:memory:");

            _options = new DbContextOptionsBuilder<UAuthAuthorizationDbContext>()
                .UseSqlite(_connection)
                .Options;
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();

            await using var db = new UAuthAuthorizationDbContext(_options);
            await db.Database.EnsureCreatedAsync();
        }

        public IRoleStore CreateStore(TenantKey tenant)
        {
            var db = new UAuthAuthorizationDbContext(_options);

            _contexts.Add(db);

            return new EfCoreRoleStore<UAuthAuthorizationDbContext>(
                db,
                new TenantExecutionContext(tenant));
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var context in _contexts)
            {
                await context.DisposeAsync();
            }

            await _connection.DisposeAsync();
        }
    }
}