using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public class ServerOptionsValidatorTests
{
    [Fact]
    public void Server_session_options_with_negative_idle_timeout_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Session.IdleTimeout = TimeSpan.FromSeconds(-5);
        });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionOptionsValidator>();

        services.AddOptions<UAuthServerOptions>().ValidateOnStart();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*Session.IdleTimeout*");
    }

    [Fact]
    public void Valid_server_session_options_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Session.Lifetime = TimeSpan.FromMinutes(30);
            o.Session.IdleTimeout = TimeSpan.FromMinutes(10);
        });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionOptionsValidator>();

        services.AddOptions<UAuthServerOptions>().ValidateOnStart();

        var provider = services.BuildServiceProvider();

        provider.Should().NotBeNull();
    }

    [Fact]
    public void Server_token_options_with_small_opaque_id_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Token.IssueOpaque = true;
            o.Token.OpaqueIdBytes = 8;
        });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerTokenOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*OpaqueIdBytes*");
    }

    [Fact]
    public void Valid_server_token_options_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Token.IssueJwt = true;
            o.Token.IssueOpaque = true;
            o.Token.AccessTokenLifetime = TimeSpan.FromMinutes(5);
            o.Token.RefreshTokenLifetime = TimeSpan.FromDays(1);
            o.Token.OpaqueIdBytes = 32;
        });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerTokenOptionsValidator>();

        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;

        options.Should().NotBeNull();
    }

    [Fact]
    public void Pkce_authorization_code_lifetime_must_be_positive()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Pkce.AuthorizationCodeLifetimeSeconds = 0;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerPkceOptionsValidator>();
        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        });

        Assert.Contains("Pkce.AuthorizationCodeLifetimeSeconds must be > 0", ex.Message);
    }

    [Fact]
    public void MultiTenant_enabled_without_resolver_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.MultiTenant.Enabled = true;
                o.MultiTenant.EnableRoute = false;
                o.MultiTenant.EnableHeader = false;
                o.MultiTenant.EnableDomain = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerMultiTenantOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*no tenant resolver is active*");
    }

    [Fact]
    public void MultiTenant_disabled_with_resolver_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.MultiTenant.Enabled = false;
                o.MultiTenant.EnableRoute = true; // no-meaning if multi-tenancy is disabled
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerMultiTenantOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*Multi-tenancy is disabled*");
    }

    [Fact]
    public void Header_enabled_without_header_name_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.MultiTenant.Enabled = true;
                o.MultiTenant.EnableHeader = true;
                o.MultiTenant.HeaderName = "";
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerMultiTenantOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*HeaderName must be specified*");
    }

    [Fact]
    public void Valid_multi_tenant_route_only_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.MultiTenant.Enabled = true;
                o.MultiTenant.EnableRoute = true;
                o.MultiTenant.EnableHeader = false;
                o.MultiTenant.EnableDomain = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerMultiTenantOptionsValidator>();

        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        options.MultiTenant.Enabled.Should().BeTrue();
        options.MultiTenant.EnableRoute.Should().BeTrue();
    }
}
