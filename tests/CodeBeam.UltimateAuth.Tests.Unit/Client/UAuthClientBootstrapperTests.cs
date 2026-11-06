using FluentAssertions;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientBootstrapperTests
{
    [Fact]
    public async Task EnsureStartedAsync_should_initialize_only_once()
    {
        var deviceId = TestDevice.Default().DeviceId!.Value.Value;

        var provider = CreateDeviceProvider(deviceId);
        var browser = new Mock<IBrowserUAuthBridge>();

        var bootstrapper = CreateBootstrapper(
            provider,
            browser,
            new UAuthClientOptions());

        await bootstrapper.EnsureStartedAsync();
        await bootstrapper.EnsureStartedAsync();

        bootstrapper.IsStarted.Should().BeTrue();

        provider.Verify(
            x => x.GetOrCreateAsync(default),
            Times.Once);

        browser.Verify(
            x => x.SetDeviceIdAsync(deviceId),
            Times.Once);

        browser.Verify(
            x => x.ConfigureAntiforgeryAsync(
                "/auth/antiforgery/token",
                null),
            Times.Once);
    }

    [Fact]
    public async Task EnsureStartedAsync_should_be_thread_safe()
    {
        var deviceId = TestDevice.Default().DeviceId!.Value.Value;

        var provider = CreateDeviceProvider(deviceId);
        var browser = new Mock<IBrowserUAuthBridge>();

        var bootstrapper = CreateBootstrapper(
            provider,
            browser,
            new UAuthClientOptions());

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => bootstrapper.EnsureStartedAsync());

        await Task.WhenAll(tasks);

        bootstrapper.IsStarted.Should().BeTrue();

        provider.Verify(
            x => x.GetOrCreateAsync(default),
            Times.Once);

        browser.Verify(
            x => x.SetDeviceIdAsync(deviceId),
            Times.Once);

        browser.Verify(
            x => x.ConfigureAntiforgeryAsync(
                "/auth/antiforgery/token",
                null),
            Times.Once);
    }

    [Fact]
    public async Task EnsureStartedAsync_should_configure_same_origin_antiforgery()
    {
        var deviceId = TestDevice.Default().DeviceId!.Value.Value;

        var provider = CreateDeviceProvider(deviceId);
        var browser = new Mock<IBrowserUAuthBridge>();

        var options = new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/identity",
                AntiforgeryToken = "/antiforgery/token"
            }
        };

        var bootstrapper = CreateBootstrapper(
            provider,
            browser,
            options);

        await bootstrapper.EnsureStartedAsync();

        browser.Verify(
            x => x.ConfigureAntiforgeryAsync(
                "/identity/antiforgery/token",
                null),
            Times.Once);
    }

    [Fact]
    public async Task EnsureStartedAsync_should_configure_cross_origin_antiforgery()
    {
        var deviceId = TestDevice.Default().DeviceId!.Value.Value;

        var provider = CreateDeviceProvider(deviceId);
        var browser = new Mock<IBrowserUAuthBridge>();

        var options = new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "https://localhost:6110/auth",
                AntiforgeryToken = "/antiforgery/token"
            }
        };

        var bootstrapper = CreateBootstrapper(
            provider,
            browser,
            options);

        await bootstrapper.EnsureStartedAsync();

        browser.Verify(
            x => x.ConfigureAntiforgeryAsync(
                "https://localhost:6110/auth/antiforgery/token",
                "https://localhost:6110"),
            Times.Once);
    }

    [Fact]
    public async Task EnsureStartedAsync_should_retry_when_antiforgery_configuration_fails()
    {
        var deviceId = TestDevice.Default().DeviceId!.Value.Value;

        var provider = CreateDeviceProvider(deviceId);
        var browser = new Mock<IBrowserUAuthBridge>();

        var attempts = 0;

        browser
            .Setup(x => x.ConfigureAntiforgeryAsync(
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(() =>
            {
                attempts++;

                if (attempts == 1)
                {
                    return new ValueTask(
                        Task.FromException(
                            new InvalidOperationException(
                                "JS initialization failed.")));
                }

                return ValueTask.CompletedTask;
            });

        var bootstrapper = CreateBootstrapper(
            provider,
            browser,
            new UAuthClientOptions());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => bootstrapper.EnsureStartedAsync());

        bootstrapper.IsStarted.Should().BeFalse();

        await bootstrapper.EnsureStartedAsync();

        bootstrapper.IsStarted.Should().BeTrue();

        browser.Verify(
            x => x.ConfigureAntiforgeryAsync(
                "/auth/antiforgery/token",
                null),
            Times.Exactly(2));
    }


    [Theory]
    [InlineData("/auth", "/auth/antiforgery/token")]
    [InlineData("auth", "auth/antiforgery/token")]
    [InlineData("/identity", "/identity/antiforgery/token")]
    public async Task EnsureStartedAsync_should_preserve_relative_hub_paths(string basePath, string expectedTokenUrl)
    {
        var deviceId = TestDevice.Default().DeviceId!.Value.Value;

        var provider = CreateDeviceProvider(deviceId);
        var browser = new Mock<IBrowserUAuthBridge>();

        var options = new UAuthClientOptions();
        options.Endpoints.BasePath = basePath;

        var bootstrapper = CreateBootstrapper(
            provider,
            browser,
            options);

        await bootstrapper.EnsureStartedAsync();

        bootstrapper.IsStarted.Should().BeTrue();

        browser.Verify(
            x => x.ConfigureAntiforgeryAsync(
                expectedTokenUrl,
                null),
            Times.Once);
    }


    private static Mock<IDeviceIdProvider> CreateDeviceProvider(
        string deviceId)
    {
        var provider = new Mock<IDeviceIdProvider>();

        provider
            .Setup(x => x.GetOrCreateAsync(default))
            .ReturnsAsync(
                TestDevice.Default().DeviceId!.Value);

        return provider;
    }

    private static UAuthClientBootstrapper CreateBootstrapper(
        Mock<IDeviceIdProvider> provider,
        Mock<IBrowserUAuthBridge> browser,
        UAuthClientOptions options)
    {
        return new UAuthClientBootstrapper(
            provider.Object,
            browser.Object,
            Options.Create(options));
    }
}
