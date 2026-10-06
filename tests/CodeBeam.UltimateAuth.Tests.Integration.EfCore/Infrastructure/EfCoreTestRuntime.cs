using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Tests.Integration.EfCore;

internal sealed class EfCoreTestRuntime : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public IServiceProvider Services { get; }

    public IntegrationTestClock Clock { get; }

    private EfCoreTestRuntime(
        SqliteConnection connection,
        IServiceProvider services,
        IntegrationTestClock clock)
    {
        _connection = connection;
        Services = services;
        Clock = clock;
    }

    public static async Task<EfCoreTestRuntime> CreateAsync(
        Action<IServiceCollection>? configureServices = null)
    {
        var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddLogging();

        // AddUltimateAuthServer registers ASP.NET Core authorization services.
        // The test runtime therefore also needs the routing infrastructure
        // normally supplied by WebApplication.
        services.AddRouting();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddUltimateAuthServer()
            .AddUltimateAuthEntityFrameworkCore(db =>
            {
                db.UseSqlite(connection);
            });

        //
        // Replace the production clock with a deterministic test clock.
        //
        var clock = new IntegrationTestClock();

        services.RemoveAll<IClock>();
        services.AddSingleton<IClock>(clock);

        //
        // Apply fault injection / test-specific overrides last.
        //
        configureServices?.Invoke(services);

        var provider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true
                });

        var runtime = new EfCoreTestRuntime(connection, provider, clock);

        try
        {
            await runtime.InitializeDatabaseAsync();

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        await using var scope =
            Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<UAuthDbContext>();

        await db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Services is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await _connection.DisposeAsync();
    }
}
