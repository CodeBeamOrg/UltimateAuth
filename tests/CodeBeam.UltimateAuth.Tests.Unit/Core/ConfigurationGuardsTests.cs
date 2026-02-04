using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Runtime;
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
    public void Configuration_Binding_Is_Also_Blocked()
    {
        var dict = new Dictionary<string, string?>
        {
            ["UltimateAuth:Session:IdleTimeout"] = "00:05:00"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
        {
            var provider = Build(services =>
            {
                services.AddUltimateAuth(config);
            });

            var _ = provider.GetRequiredService<IOptions<UAuthOptions>>().Value;
        });
    }

    private sealed class FakeServerMarker : IUAuthRuntimeMarker { }

    private static IServiceProvider Build(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }
}
