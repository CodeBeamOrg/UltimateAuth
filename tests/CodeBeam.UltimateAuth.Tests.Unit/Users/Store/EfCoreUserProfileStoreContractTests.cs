using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public sealed class EfCoreUserProfileStoreContractTests : UserProfileStoreContractTests
{
    private readonly IUAuthPaginationPolicy _pagination = new UAuthPaginationOptions();

    protected override async Task<IUserProfileStoreTestDatabase> CreateDatabaseAsync()
    {
        var database = new Database(_pagination);
        await database.InitializeAsync();
        return database;
    }

    private sealed class Database : IUserProfileStoreTestDatabase
    {
        private readonly SqliteConnection _connection;
        private readonly UAuthUserDbContext _db;
        private readonly IUAuthPaginationPolicy _pagination;

        public Database(IUAuthPaginationPolicy pagination)
        {
            _connection =
                new SqliteConnection("Data Source=:memory:");

            var options =
                new DbContextOptionsBuilder<UAuthUserDbContext>()
                    .UseSqlite(_connection)
                    .Options;

            _db = new UAuthUserDbContext(options);
            _pagination = pagination;
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
                new TenantExecutionContext(tenant), _pagination);
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
