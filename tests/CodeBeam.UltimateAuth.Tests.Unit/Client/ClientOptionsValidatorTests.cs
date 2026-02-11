using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Options;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ClientOptionsValidatorTests
{
    [Fact]
    public void ClientProfile_not_specified_and_autodetect_disabled_should_fail()
    {
        var services = new ServiceCollection();

        services.AddOptions<UAuthClientOptions>()
            .Configure(o =>
            {
                o.ClientProfile = UAuthClientProfile.NotSpecified;
                o.AutoDetectClientProfile = false;
            });

        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientOptionsValidator>();
        var provider = services.BuildServiceProvider();
        Action act = () => _ = provider.GetRequiredService<IOptions<UAuthClientOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().WithMessage("*ClientProfile*AutoDetectClientProfile*");
    }

    [Fact]
    public void ClientEndpoint_basepath_empty_should_fail()
    {
        var services = new ServiceCollection();

        services.AddOptions<UAuthClientOptions>()
            .Configure(o =>
            {
                o.Endpoints.BasePath = "";
            });

        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientEndpointOptionsValidator>();
        var provider = services.BuildServiceProvider();
        Action act = () =>_ = provider.GetRequiredService<IOptions<UAuthClientOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().WithMessage("*BasePath*");
    }

    [Fact]
    public void Valid_client_options_should_pass()
    {
        var services = new ServiceCollection();

        services.AddOptions<UAuthClientOptions>()
            .Configure(o =>
            {
                o.ClientProfile = UAuthClientProfile.BlazorWasm;
                o.AutoDetectClientProfile = false;
                o.Endpoints.BasePath = "/auth";
            });

        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientEndpointOptionsValidator>();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<UAuthClientOptions>>().Value;
        options.ClientProfile.Should().Be(UAuthClientProfile.BlazorWasm);
    }
}
