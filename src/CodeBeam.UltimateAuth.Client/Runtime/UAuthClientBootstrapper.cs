using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Client.Infrastructure;

// DeviceId is automatically created and managed by UAuthClientProvider. This class is for advanced situations.
namespace CodeBeam.UltimateAuth.Client.Runtime
{
    internal sealed class UAuthClientBootstrapper : IUAuthClientBootstrapper
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _started;

        private readonly IDeviceIdProvider _deviceIdProvider;
        private readonly IBrowserUAuthBridge _browser;
        private readonly ISessionCoordinator _coordinator;

        public bool IsStarted => _started;

        public UAuthClientBootstrapper(
            IDeviceIdProvider deviceIdProvider,
            IBrowserUAuthBridge browser,
            ISessionCoordinator coordinator)
        {
            _deviceIdProvider = deviceIdProvider;
            _browser = browser;
            _coordinator = coordinator;
        }

        public async Task EnsureStartedAsync()
        {
            if (_started)
                return;

            await _gate.WaitAsync();
            try
            {
                if (_started)
                    return;

                var deviceId = await _deviceIdProvider.GetOrCreateAsync();
                await _browser.SetDeviceIdAsync(deviceId.Value);
                await _coordinator.StartAsync();

                _started = true;
            }
            finally
            {
                _gate.Release();
            }
        }

    }
}
