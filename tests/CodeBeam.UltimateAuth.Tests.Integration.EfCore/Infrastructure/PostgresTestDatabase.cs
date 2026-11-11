using Npgsql;
using Testcontainers.PostgreSql;

namespace CodeBeam.UltimateAuth.Tests.Integration.EfCore;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        ConnectionString =
            Environment.GetEnvironmentVariable("UAUTH_TEST_POSTGRES") ?? "";

        if (!string.IsNullOrWhiteSpace(ConnectionString))
            return;

        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .Build();

        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public async Task<PostgresTestDatabase> CreateDatabaseAsync()
    {
        var name = "uauth_test_" + Guid.NewGuid().ToString("N");

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command =
            new NpgsqlCommand($"CREATE DATABASE {name}", connection);

        await command.ExecuteNonQueryAsync();

        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = name,
            Pooling = false
        };

        return new PostgresTestDatabase(
            builder.ConnectionString,
            ConnectionString,
            name);
    }
}

public sealed class PostgresTestDatabase(string connectionString, string adminConnectionString, string name) : IAsyncDisposable
{
    public string ConnectionString => connectionString;

    public async ValueTask DisposeAsync()
    {
        await using var connection =
            new NpgsqlConnection(adminConnectionString);

        await connection.OpenAsync();

        await using var command = new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", connection);

        await command.ExecuteNonQueryAsync();
    }
}
