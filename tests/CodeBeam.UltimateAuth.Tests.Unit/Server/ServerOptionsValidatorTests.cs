using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Server.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public class ServerOptionsValidatorTests
{
    [Fact]
    public void Server_session_options_with_negative_idle_timeout_should_fail()
    {
        var services = new ServiceCollection();

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

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Tokens.IssueOpaque = true;
            o.Tokens.OpaqueIdBytes = 8;
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

        services.AddUltimateAuth();
        services.AddUltimateAuthServer(o =>
        {
            o.Tokens.IssueJwt = true;
            o.Tokens.IssueOpaque = true;
            o.Tokens.AccessTokenLifetime = TimeSpan.FromMinutes(5);
            o.Tokens.RefreshTokenLifetime = TimeSpan.FromDays(1);
            o.Tokens.OpaqueIdBytes = 32;
        });

        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerTokenOptionsValidator>();

        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;

        options.Should().NotBeNull();
    }

}
