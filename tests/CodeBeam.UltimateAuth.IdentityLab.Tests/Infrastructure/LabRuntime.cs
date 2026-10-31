using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.IdentityLab.Policy;
using CodeBeam.UltimateAuth.IdentityLab.Seed;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Infrastructure;

internal sealed class LabRuntime(ServiceProvider provider) : IAsyncDisposable
{
    public ServiceProvider Services => provider;

    public static async Task<LabRuntime> CreateAsync(LabDatabase database)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        services.AddUltimateAuthServer().AddUltimateAuthEntityFrameworkCore(options =>
            options.UseNpgsql(database.ConnectionString, pg => pg.MigrationsAssembly(typeof(LabDbContext).Assembly.FullName)));
        services.RemoveAll<IClock>();
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<IDbContextFactory<LabDbContext>>(database);
        services.AddScoped<ILabRootResolver, LabRootResolver>();
        services.AddScoped<LabProvisioningRollbackGate>();
        services.AddScoped<IAccessPolicy, SandboxUAuthAccessPolicy>();
        services.AddScoped<IAccessPolicy, LabApplicationPolicy>();
        services.Configure<LabAdminOptions>(options =>
        {
            options.Username = "lab-admin";
            options.Email = "lab-admin@example.test";
            options.Password = "Lab-admin-test-password-8!";
        });
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var runtime = new LabRuntime(provider);
        try
        {
            await using var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<UAuthDbContext>().Database.MigrateAsync();
            await ActivatorUtilities.CreateInstance<LabAdminSeedContributor>(scope.ServiceProvider).SeedAsync(TenantKeys.Single);
            await ActivatorUtilities.CreateInstance<LabDemoRoleSeedContributor>(scope.ServiceProvider).SeedAsync(TenantKeys.Single);
            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }

    public ValueTask DisposeAsync() => provider.DisposeAsync();

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
