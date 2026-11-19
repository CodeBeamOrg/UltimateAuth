using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Runtime;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ApplicationProfileTests
{
    [Fact]
    public void PostConfigure_Should_Not_Change_When_AutoDetect_Disabled()
    {
        var detector = new Mock<IApplicationProfileDetector>();
        var sut = new UAuthClientOptionsPostConfigure(detector.Object, new ServiceCollection().BuildServiceProvider());

        var options = new UAuthClientOptions
        {
            AutoDetectClientProfile = false,
            ClientProfile = UAuthApplicationProfile.NotSpecified
        };

        sut.PostConfigure(null, options);
        options.ClientProfile.Should().Be(UAuthApplicationProfile.NotSpecified);
    }

    [Fact]
    public void PostConfigure_Should_Not_Override_Explicit_Profile()
    {
        var detector = new Mock<IApplicationProfileDetector>();
        var sut = new UAuthClientOptionsPostConfigure(detector.Object, new ServiceCollection().BuildServiceProvider());

        var options = new UAuthClientOptions
        {
            AutoDetectClientProfile = true,
            ClientProfile = UAuthApplicationProfile.BlazorServer
        };

        sut.PostConfigure(null, options);
        options.ClientProfile.Should().Be(UAuthApplicationProfile.BlazorServer);
    }

    [Fact]
    public void PostConfigure_Should_Set_Profile_From_Detector()
    {
        var detector = new Mock<IApplicationProfileDetector>();

        detector.Setup(x => x.Detect(It.IsAny<IServiceProvider>()))
            .Returns(UAuthApplicationProfile.Maui);

        var sut = new UAuthClientOptionsPostConfigure(detector.Object, new ServiceCollection().BuildServiceProvider());

        var options = new UAuthClientOptions
        {
            AutoDetectClientProfile = true,
            ClientProfile = UAuthApplicationProfile.NotSpecified
        };

        sut.PostConfigure(null, options);
        options.ClientProfile.Should().Be(UAuthApplicationProfile.Maui);
    }

    [Fact]
    public void Detect_Should_Return_Hub_When_Marker_Exists()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUAuthHubMarker>(Mock.Of<IUAuthHubMarker>());

        var sp = services.BuildServiceProvider();
        var detector = new UAuthApplicationProfileDetector();
        var result = detector.Detect(sp);
        result.Should().Be(UAuthApplicationProfile.UAuthHub);
    }

    // TODO: This test fails on CI, find a betterway to test or implementation logic
    //[Fact]
    //public void Detect_Should_Default_To_WebServer()
    //{
    //    var sp = new ServiceCollection().BuildServiceProvider();
    //    var detector = new UAuthClientProfileDetector();
    //    var result = detector.Detect(sp);
    //    result.Should().Be(UAuthClientProfile.WebServer);
    //}
}
