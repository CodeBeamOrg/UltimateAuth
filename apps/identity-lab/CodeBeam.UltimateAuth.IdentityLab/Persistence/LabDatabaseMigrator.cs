using CodeBeam.UltimateAuth.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Persistence;

public static class LabDatabaseMigrator
{
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("IdentityLab.Migrations");

        logger.LogInformation("Starting Identity Lab database migrations.");

        var uauthDb = scope.ServiceProvider.GetRequiredService<UAuthDbContext>();

        await uauthDb.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("UltimateAuth database migrations completed.");

        var labDbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LabDbContext>>();

        await using var labDb = await labDbFactory.CreateDbContextAsync(cancellationToken);

        await labDb.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Identity Lab database migrations completed.");
    }
}
