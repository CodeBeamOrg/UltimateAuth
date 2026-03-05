using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

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

    [Fact]
    public void UserIdentifiers_both_admin_and_user_override_disabled_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Identifiers.AllowAdminOverride = false;
                o.Identifiers.AllowUserOverride = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerUserIdentifierOptionsValidator>();

        var provider = services.BuildServiceProvider();

        Action act = () =>
        {
            _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        };

        act.Should().Throw<OptionsValidationException>().WithMessage("*AllowAdminOverride and AllowUserOverride*");
    }

    [Fact]
    public void UserIdentifiers_at_least_one_override_enabled_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.Identifiers.AllowAdminOverride = true;
                o.Identifiers.AllowUserOverride = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerUserIdentifierOptionsValidator>();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        options.Identifiers.AllowAdminOverride.Should().BeTrue();
    }

    [Fact]
    public void No_session_resolver_enabled_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.SessionResolution.EnableBearer = false;
                o.SessionResolution.EnableHeader = false;
                o.SessionResolution.EnableCookie = false;
                o.SessionResolution.EnableQuery = false;
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionResolutionOptionsValidator>();
        var provider = services.BuildServiceProvider();
        Action act = () => _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().WithMessage("*At least one session resolver must be enabled*");
    }

    [Fact]
    public void Disabled_resolver_in_order_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.SessionResolution.EnableBearer = true;
                o.SessionResolution.EnableQuery = false;
                o.SessionResolution.Order = new() { "Bearer", "Query" };
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionResolutionOptionsValidator>();
        var provider = services.BuildServiceProvider();
        Action act = () => _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().WithMessage("*not enabled*");
    }

    [Fact]
    public void Header_enabled_without_name_should_fail()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.SessionResolution.EnableHeader = true;
                o.SessionResolution.HeaderName = "";
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionResolutionOptionsValidator>();
        var provider = services.BuildServiceProvider();
        Action act = () => _ = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().WithMessage("*HeaderName*");
    }

    [Fact]
    public void Valid_session_resolution_should_pass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        services.AddOptions<UAuthServerOptions>()
            .Configure(o =>
            {
                o.SessionResolution.EnableBearer = true;
                o.SessionResolution.EnableHeader = false;
                o.SessionResolution.EnableCookie = false;
                o.SessionResolution.EnableQuery = false;
                o.SessionResolution.Order = new() { "Bearer" };
            });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionResolutionOptionsValidator>();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        options.SessionResolution.EnableBearer.Should().BeTrue();
    }
}
