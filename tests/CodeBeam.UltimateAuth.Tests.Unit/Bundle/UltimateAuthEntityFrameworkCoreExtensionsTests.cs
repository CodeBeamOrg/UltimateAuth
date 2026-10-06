using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit.EntityFrameworkCore;

public sealed class UltimateAuthEntityFrameworkCoreExtensionsTests
{
    [Fact]
    public void AddUltimateAuthEntityFrameworkCore_WithUnifiedContext_ShouldRegisterUAuthDbContext()
    {
        var services = new ServiceCollection();

        services.AddUltimateAuthEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();

        var context =
            scope.ServiceProvider.GetRequiredService<UAuthDbContext>();

        context.Should().NotBeNull();
    }

    [Fact]
    public void AddUltimateAuthEntityFrameworkCore_WithUnifiedContext_ShouldConfigureDatabaseProvider()
    {
        var services = new ServiceCollection();

        services.AddUltimateAuthEntityFrameworkCore(options =>
            options.UseInMemoryDatabase("uauth-test"));

        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();

        var context =
            scope.ServiceProvider.GetRequiredService<UAuthDbContext>();

        context.Database.ProviderName
            .Should()
            .Be("Microsoft.EntityFrameworkCore.InMemory");
    }

    [Fact]
    public void AddUltimateAuthEntityFrameworkCore_WithDefaultConfiguration_ShouldResolveAllContexts()
    {
        var services = new ServiceCollection();

        services.AddUltimateAuthEntityFrameworkCore(options =>
        {
            options.Default = builder =>
                builder.UseInMemoryDatabase(
                    Guid.NewGuid().ToString());
        });

        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();

        scope.ServiceProvider
            .GetRequiredService<UAuthUserDbContext>()
            .Should().NotBeNull();

        scope.ServiceProvider
            .GetRequiredService<UAuthCredentialDbContext>()
            .Should().NotBeNull();

        scope.ServiceProvider
            .GetRequiredService<UAuthAuthorizationDbContext>()
            .Should().NotBeNull();

        scope.ServiceProvider
            .GetRequiredService<UAuthSessionDbContext>()
            .Should().NotBeNull();

        scope.ServiceProvider
            .GetRequiredService<UAuthTokenDbContext>()
            .Should().NotBeNull();

        scope.ServiceProvider
            .GetRequiredService<UAuthAuthenticationDbContext>()
            .Should().NotBeNull();
    }

    [Fact]
    public void AddUltimateAuthEntityFrameworkCore_SpecificConfiguration_ShouldOverrideDefault()
    {
        var services = new ServiceCollection();

        services.AddUltimateAuthEntityFrameworkCore(options =>
        {
            options.Default = builder =>
                builder.UseSqlite("Data Source=default.db");

            options.Users = builder =>
                builder.UseSqlite("Data Source=users.db");
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var users =
            scope.ServiceProvider
                .GetRequiredService<UAuthUserDbContext>();

        var sessions =
            scope.ServiceProvider
                .GetRequiredService<UAuthSessionDbContext>();

        users.Database.GetDbConnection()
            .DataSource
            .Should()
            .Be("users.db");

        sessions.Database.GetDbConnection()
            .DataSource
            .Should()
            .Be("default.db");
    }
}
