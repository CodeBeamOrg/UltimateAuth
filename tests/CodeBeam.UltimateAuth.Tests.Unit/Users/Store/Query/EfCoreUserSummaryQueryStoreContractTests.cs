
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public sealed class EfCoreUserSummaryQueryStoreContractTests : UserSummaryQueryStoreContractTests
{
    protected override async Task<IUserSummaryQueryStoreTestDatabase> CreateDatabaseAsync()
    {
        var db = new Database();
        await db.InitializeAsync();
        return db;
    }

    private sealed class Database : IUserSummaryQueryStoreTestDatabase
    {
        private readonly SqliteConnection _connection;
        private readonly UAuthUserDbContext _db;
        private readonly IUAuthPaginationPolicy _pagination = new UAuthPaginationOptions();

        public Database()
        {
            _connection = new SqliteConnection(
                "Data Source=:memory:");

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

        public IUserSummaryQueryStore CreateStore(
            TenantKey tenant)
        {
            return new EfCoreUserSummaryQueryStore<UAuthUserDbContext>(
                _db,
                new TenantExecutionContext(tenant),
                _pagination);
        }

        public async Task SeedAsync(
            TenantKey tenant,
            IReadOnlyList<UserSummarySeed> users)
        {
            const int batchSize = 500;

            foreach (var batch in users.Chunk(batchSize))
            {
                foreach (var user in batch)
                {
                    _db.Lifecycles.Add(new UserLifecycleProjection
                    {
                        Id = Guid.NewGuid(),
                        Tenant = tenant,
                        UserKey = user.UserKey,
                        Status = user.Status,
                        CreatedAt = user.CreatedAt,
                        DeletedAt = user.IsDeleted
                            ? user.CreatedAt.AddMinutes(1)
                            : null
                    });

                    if (user.HasProfile)
                    {
                        _db.Profiles.Add(new UserProfileProjection
                        {
                            Id = Guid.NewGuid(),
                            Tenant = tenant,
                            UserKey = user.UserKey,
                            ProfileKey = user.ProfileKey,
                            DisplayName = user.DisplayName,
                            CreatedAt = user.CreatedAt
                        });
                    }

                    AddIdentifier(
                        tenant,
                        user,
                        UserIdentifierType.Username,
                        user.UserName);

                    AddIdentifier(
                        tenant,
                        user,
                        UserIdentifierType.Email,
                        user.Email);

                    AddIdentifier(
                        tenant,
                        user,
                        UserIdentifierType.Phone,
                        user.Phone);
                }

                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
            }
        }

        private void AddIdentifier(
            TenantKey tenant,
            UserSummarySeed user,
            UserIdentifierType type,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            _db.Identifiers.Add(new UserIdentifierProjection
            {
                Id = Guid.NewGuid(),
                Tenant = tenant,
                UserKey = user.UserKey,
                Type = type,
                Value = value,
                NormalizedValue = value.ToLowerInvariant(),
                IsPrimary = true,
                CreatedAt = user.CreatedAt
            });
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
