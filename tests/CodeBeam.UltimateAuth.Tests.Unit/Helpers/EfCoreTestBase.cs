using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

public abstract class EfCoreTestBase
{
    protected SqliteConnection CreateOpenConnection()
    {
        var conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        return conn;
    }

    protected static TDbContext CreateDbContext<TDbContext>(SqliteConnection connection, Func<DbContextOptions<TDbContext>, TDbContext> factory) where TDbContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var db = factory(options);
        db.Database.EnsureCreated();
        return db;
    }
}
