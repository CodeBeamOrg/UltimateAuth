namespace CodeBeam.UltimateAuth.Client.Infrastructure;

// TODO: Add device id auto creation for MVC, this is only for blazor.
internal sealed class UAuthClientBootstrapper : IUAuthClientBootstrapper
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _started;

    private readonly IDeviceIdProvider _deviceIdProvider;
    private readonly IBrowserUAuthBridge _browser;

    public bool IsStarted => _started;

    public UAuthClientBootstrapper(IDeviceIdProvider deviceIdProvider, IBrowserUAuthBridge browser)
    {
        _deviceIdProvider = deviceIdProvider;
        _browser = browser;
    }

    public async Task EnsureStartedAsync(CancellationToken ct = default)
    {
        if (_started)
            return;

        await _gate.WaitAsync(ct);
        try
        {
            if (_started)
                return;

            var deviceId = await _deviceIdProvider.GetOrCreateAsync();
            await _browser.SetDeviceIdAsync(deviceId.Value);

            _started = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
