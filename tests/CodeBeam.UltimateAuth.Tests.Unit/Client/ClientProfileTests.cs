using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Runtime;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ClientProfileTests
{
    [Fact]
    public void PostConfigure_Should_Not_Change_When_AutoDetect_Disabled()
    {
        var detector = new Mock<IClientProfileDetector>();
        var sut = new UAuthClientOptionsPostConfigure(detector.Object, new ServiceCollection().BuildServiceProvider());

        var options = new UAuthClientOptions
        {
            AutoDetectClientProfile = false,
            ClientProfile = UAuthClientProfile.NotSpecified
        };

        sut.PostConfigure(null, options);
        options.ClientProfile.Should().Be(UAuthClientProfile.NotSpecified);
    }

    [Fact]
    public void PostConfigure_Should_Not_Override_Explicit_Profile()
    {
        var detector = new Mock<IClientProfileDetector>();
        var sut = new UAuthClientOptionsPostConfigure(detector.Object, new ServiceCollection().BuildServiceProvider());

        var options = new UAuthClientOptions
        {
            AutoDetectClientProfile = true,
            ClientProfile = UAuthClientProfile.BlazorServer
        };

        sut.PostConfigure(null, options);
        options.ClientProfile.Should().Be(UAuthClientProfile.BlazorServer);
    }

    [Fact]
    public void PostConfigure_Should_Set_Profile_From_Detector()
    {
        var detector = new Mock<IClientProfileDetector>();

        detector.Setup(x => x.Detect(It.IsAny<IServiceProvider>()))
            .Returns(UAuthClientProfile.Maui);

        var sut = new UAuthClientOptionsPostConfigure(detector.Object, new ServiceCollection().BuildServiceProvider());

        var options = new UAuthClientOptions
        {
            AutoDetectClientProfile = true,
            ClientProfile = UAuthClientProfile.NotSpecified
        };

        sut.PostConfigure(null, options);
        options.ClientProfile.Should().Be(UAuthClientProfile.Maui);
    }

    [Fact]
    public void Detect_Should_Return_Hub_When_Marker_Exists()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUAuthHubMarker>(Mock.Of<IUAuthHubMarker>());

        var sp = services.BuildServiceProvider();
        var detector = new UAuthClientProfileDetector();
        var result = detector.Detect(sp);
        result.Should().Be(UAuthClientProfile.UAuthHub);
    }

    [Fact]
    public void Detect_Should_Default_To_WebServer()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var detector = new UAuthClientProfileDetector();
        var result = detector.Detect(sp);
        result.Should().Be(UAuthClientProfile.WebServer);
    }
}
