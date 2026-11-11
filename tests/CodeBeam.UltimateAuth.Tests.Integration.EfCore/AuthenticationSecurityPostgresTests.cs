using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CodeBeam.UltimateAuth.Tests.Integration.EfCore.Authentication;

public sealed class AuthenticationSecurityPostgresTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Account_state_must_be_unique_when_credential_type_is_null()
    {
        await using var database = await postgres.CreateDatabaseAsync();

        var options =
            new DbContextOptionsBuilder<UAuthAuthenticationDbContext>()
                .UseNpgsql(database.ConnectionString)
                .Options;

        await using (var db = new UAuthAuthenticationDbContext(options))
        {
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        await using var connection =
            new NpgsqlConnection(database.ConnectionString);

        await connection.OpenAsync(TestContext.Current.CancellationToken);

        const string insert = """
            INSERT INTO "UAuth_Authentication"
                ("Id", "Tenant", "UserKey", "Scope",
                 "CredentialType", "FailedAttempts",
                 "RequiresReauthentication", "ResetAttempts",
                 "SecurityVersion")
            VALUES
                (@id, 'default', 'test-user', 0,
                 NULL, 0, FALSE, 0, 0);
            """;

        async Task InsertAsync()
        {
            await using var command = new NpgsqlCommand(insert, connection);
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            await command.ExecuteNonQueryAsync();
        }

        await InsertAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(
            InsertAsync);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
    }
}
