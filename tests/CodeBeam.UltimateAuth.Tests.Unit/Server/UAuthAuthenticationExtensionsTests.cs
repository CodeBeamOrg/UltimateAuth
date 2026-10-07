using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Server.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthAuthenticationExtensionsTests
{
    [Fact]
    public async Task AddUAuthScheme_ShouldRegisterGlobalScheme()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services
            .AddAuthentication()
            .AddUAuthScheme();

        await using var provider = services.BuildServiceProvider();

        var schemes =
            provider.GetRequiredService<IAuthenticationSchemeProvider>();

        var scheme = await schemes.GetSchemeAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        scheme.Should().NotBeNull();
        scheme!.HandlerType.Should()
            .Be(typeof(UAuthAuthenticationHandler));
    }

    [Fact]
    public async Task AddUAuthResourceApi_ShouldRegisterGlobalScheme()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services
            .AddAuthentication()
            .AddUAuthResourceApi();

        await using var provider = services.BuildServiceProvider();

        var schemes =
            provider.GetRequiredService<IAuthenticationSchemeProvider>();

        var scheme = await schemes.GetSchemeAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        scheme.Should().NotBeNull();
        scheme!.HandlerType.Should()
            .Be(typeof(UAuthResourceAuthenticationHandler));
    }

    [Fact]
    public void AddUAuthScheme_ShouldApplyConfiguration()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services
            .AddAuthentication()
            .AddUAuthScheme(options =>
            {
                options.ClaimsIssuer = "UltimateAuth.Tests";
            });

        using var provider = services.BuildServiceProvider();

        var options =
            provider.GetRequiredService<
                Microsoft.Extensions.Options.IOptionsMonitor<
                    UAuthAuthenticationSchemeOptions>>();

        var configured = options.Get(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        configured.ClaimsIssuer.Should().Be("UltimateAuth.Tests");
    }
}
