using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Core;

public sealed class ConfigurationGuardsTests
{
    [Fact]
    public void Default_No_Config_Passes()
    {
        var provider = Build(services =>
        {
            services.AddUltimateAuth();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        });

        var _ = provider.GetRequiredService<IOptions<UAuthOptions>>().Value;
    }

    [Fact]
    public void Direct_Config_With_Allow_Passes()
    {
        var provider = Build(services =>
        {
            services.AddUltimateAuth(o =>
            {
                o.AllowDirectCoreConfiguration = true;
                o.Session.IdleTimeout = TimeSpan.FromMinutes(5);
            });
        });

        var options = provider.GetRequiredService<IOptions<UAuthOptions>>().Value;

        Assert.Equal(TimeSpan.FromMinutes(5), options.Session.IdleTimeout);
    }

    [Fact]
    public void Direct_Config_Without_Allow_Fails()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var provider = Build(services =>
            {
                services.AddUltimateAuth(o =>
                {
                    o.Session.IdleTimeout = TimeSpan.FromMinutes(5);
                });
            });

            var _ = provider.GetRequiredService<IOptions<UAuthOptions>>().Value;
        });
    }

    [Fact]
    public void Server_Without_Core_Config_Passes()
    {
        var provider = Build(services =>
        {
            services.AddUltimateAuth();
            services.AddSingleton<IUAuthRuntimeMarker, FakeServerMarker>();
        });

        var _ = provider.GetRequiredService<IOptions<UAuthOptions>>().Value;
    }

    [Fact]
    public void Server_With_Core_Config_Fails()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var provider = Build(services =>
            {
                services.AddUltimateAuth(o =>
                {
                    o.Session.IdleTimeout = TimeSpan.FromMinutes(5);
                });

                services.AddSingleton<IUAuthRuntimeMarker, FakeServerMarker>();
            });

            var _ = provider.GetRequiredService<IOptions<UAuthOptions>>().Value;
        });
    }

    [Fact]
    public void Server_With_Core_Config_Even_With_Allow_Fails()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var provider = Build(services =>
            {
                services.AddUltimateAuth(o =>
                {
                    o.AllowDirectCoreConfiguration = true;
                    o.Session.IdleTimeout = TimeSpan.FromMinutes(5);
                });

                services.AddSingleton<IUAuthRuntimeMarker, FakeServerMarker>();
            });

            var _ = provider.GetRequiredService<IOptions<UAuthOptions>>().Value;
        });
    }

    [Fact]
    public void Core_configuration_is_blocked_when_server_is_present()
    {
        var dict = new Dictionary<string, string?>
        {
            ["UltimateAuth:Core:Session:IdleTimeout"] = "00:05:00"
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        var provider = Build(services =>
        {
            services.AddSingleton<IConfiguration>(config);
            services.AddUltimateAuth();
            services.AddUltimateAuthServer();
        });

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthOptions>>().Value;
        };

        act.Should().Throw<InvalidOperationException>().WithMessage("*Direct core configuration is not allowed*");
    }

    [Fact]
    public void Core_configuration_is_allowed_when_server_is_not_present()
    {
        var dict = new Dictionary<string, string?>
        {
            ["UltimateAuth:Core:AllowDirectCoreConfiguration"] = "true",
            ["UltimateAuth:Core:Session:IdleTimeout"] = "00:05:00"
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        var provider = Build(services =>
        {
            services.AddSingleton<IConfiguration>(config);
            services.AddUltimateAuth();
        });

        var options = provider.GetRequiredService<IOptions<UAuthOptions>>().Value;
        options.Session.IdleTimeout.Should().Be(TimeSpan.FromMinutes(5));
    }

    private sealed class FakeServerMarker : IUAuthRuntimeMarker { }

    private static IServiceProvider Build(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        configure(services);
        return services.BuildServiceProvider();
    }
}
