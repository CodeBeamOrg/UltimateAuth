using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? container;
    private string connectionString = "";
    public async Task InitializeAsync()
    {
        connectionString = Environment.GetEnvironmentVariable("IDENTITYLAB_TEST_POSTGRES") ?? "";
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await container.StartAsync();
            connectionString = container.GetConnectionString();
        }
    }
    public async Task<LabDatabase> CreateAsync()
    {
        // A separate database per test prevents state leaking across tests or classes.
        var name = "identitylab_test_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE {name}", connection);
        await command.ExecuteNonQueryAsync();
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = name, Pooling = false };
        var database = new LabDatabase(builder.ConnectionString, connectionString, name);
        await using var db = database.CreateDbContext();
        await db.Database.MigrateAsync();
        return database;
    }
    public async Task DisposeAsync()
    {
        if (container is not null) await container.DisposeAsync();
    }
}

public sealed class LabDatabase(string connectionString, string adminConnection, string name)
    : IDbContextFactory<LabDbContext>, IAsyncDisposable
{
    public string ConnectionString => connectionString;
    public static readonly UserKey OwnerA = UserKey.FromString("owner-a");
    public static readonly UserKey OwnerB = UserKey.FromString("owner-b");
    public static readonly UserKey MemberA = UserKey.FromString("member-a");
    public static readonly UserKey MemberB = UserKey.FromString("member-b");
    public LabDbContext CreateDbContext() => new(new DbContextOptionsBuilder<LabDbContext>()
        .UseNpgsql(connectionString, options => options.MigrationsHistoryTable("__IdentityLabMigrationsHistory")).Options);
    public Task<LabDbContext> CreateDbContextAsync(CancellationToken ct = default)
        => Task.FromResult(CreateDbContext());
    public async Task SeedAsync()
    {
        var service = new SandboxService(this);
        await service.CreateAsync(OwnerA, "Sandbox A");
        await service.CreateAsync(OwnerB, "Sandbox B");
        await service.AddMemberAsync(OwnerA, MemberA);
        await service.AddMemberAsync(OwnerB, MemberB);
    }
    public async Task SetStatusAsync(SandboxStatus status)
    {
        await using var db = CreateDbContext();
        var sandbox = await db.Sandboxes.SingleAsync(x => x.OwnerUserKey == OwnerA);
        sandbox.Status = status;
        await db.SaveChangesAsync();
    }
    public async Task<string[]> SnapshotAsync()
    {
        await using var db = CreateDbContext();
        var sandboxes = await db.Sandboxes.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        var members = await db.SandboxMembers.AsNoTracking().OrderBy(x => x.UserKey).ToListAsync();
        var settings = await db.SandboxSettings.AsNoTracking().OrderBy(x => x.SandboxId).ToListAsync();
        return sandboxes.Select(x => $"S:{x.Id}:{x.OwnerUserKey}:{x.Name}:{x.Status}")
            .Concat(members.Select(x => $"M:{x.Id}:{x.SandboxId}:{x.UserKey}:{x.Role}"))
            .Concat(settings.Select(x => $"C:{x.SandboxId}:{x.MaxUsers}")).ToArray();
    }
    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
