using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Options;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ClientOptionsValidatorTests
{
    [Fact]
    public void ApplicationProfile_not_specified_and_autodetect_disabled_should_fail()
    {
        var services = new ServiceCollection();

        services.AddOptions<UAuthClientOptions>()
            .Configure(o =>
            {
                o.ApplicationProfile = UAuthApplicationProfile.NotSpecified;
                o.AutoDetectApplicationProfile = false;
            });

        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientOptionsValidator>();
        var provider = services.BuildServiceProvider();
        Action act = () => _ = provider.GetRequiredService<IOptions<UAuthClientOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().WithMessage("*ApplicationProfile*AutoDetectApplicationProfile*");
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
                o.ApplicationProfile = UAuthApplicationProfile.BlazorWasm;
                o.AutoDetectApplicationProfile = false;
                o.Endpoints.BasePath = "/auth";
            });

        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientEndpointOptionsValidator>();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<UAuthClientOptions>>().Value;
        options.ApplicationProfile.Should().Be(UAuthApplicationProfile.BlazorWasm);
    }

    [Fact]
    public void Marker_Should_Not_Throw_On_First_Call()
    {
        var marker = new ClientConfigurationMarker();
        var act = () => marker.MarkConfigured();
        act.Should().NotThrow();
    }

    [Fact]
    public void Marker_Should_Throw_When_Configured_Twice()
    {
        var marker = new ClientConfigurationMarker();
        marker.MarkConfigured();
        Action act = () => marker.MarkConfigured();
        act.Should().Throw<InvalidOperationException>();
    }
}
