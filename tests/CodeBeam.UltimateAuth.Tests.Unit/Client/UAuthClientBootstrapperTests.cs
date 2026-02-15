using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientBootstrapperTests
{
    [Fact]
    public async Task EnsureStartedAsync_should_initialize_only_once()
    {
        var deviceId = TestDevice.Default().DeviceId!.Value;

        var provider = new Mock<IDeviceIdProvider>();
        provider.Setup(x => x.GetOrCreateAsync(default)).ReturnsAsync(deviceId);

        var browser = new Mock<IBrowserUAuthBridge>();

        var bootstrapper = new UAuthClientBootstrapper(provider.Object, browser.Object);

        await bootstrapper.EnsureStartedAsync();
        await bootstrapper.EnsureStartedAsync();

        provider.Verify(x => x.GetOrCreateAsync(default), Times.Once);
        browser.Verify(x => x.SetDeviceIdAsync(deviceId.Value), Times.Once);
    }

    [Fact]
    public async Task EnsureStartedAsync_should_be_thread_safe()
    {
        var deviceId = TestDevice.Default().DeviceId!.Value;

        var provider = new Mock<IDeviceIdProvider>();
        provider.Setup(x => x.GetOrCreateAsync(default)).ReturnsAsync(deviceId);

        var browser = new Mock<IBrowserUAuthBridge>();
        var bootstrapper = new UAuthClientBootstrapper(provider.Object, browser.Object);

        var tasks = Enumerable.Range(0, 10).Select(_ => bootstrapper.EnsureStartedAsync());

        await Task.WhenAll(tasks);

        provider.Verify(x => x.GetOrCreateAsync(default), Times.Once);
        browser.Verify(x => x.SetDeviceIdAsync(deviceId.Value), Times.Once);
    }
}
