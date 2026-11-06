using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

// TODO: Add device id auto creation for MVC, this is only for blazor.
// TODO: Support dynamic tenant changes during the application lifetime.
// Antiforgery configuration is currently initialized only once.
// Reconfigure the token URL when the active tenant changes.
internal sealed class UAuthClientBootstrapper : IUAuthClientBootstrapper
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _started;

    private readonly IDeviceIdProvider _deviceIdProvider;
    private readonly IBrowserUAuthBridge _browser;
    private readonly UAuthClientOptions _options;

    public bool IsStarted => _started;

    public UAuthClientBootstrapper(IDeviceIdProvider deviceIdProvider, IBrowserUAuthBridge browser, IOptions<UAuthClientOptions> options)
    {
        _deviceIdProvider = deviceIdProvider;
        _browser = browser;
        _options = options.Value;
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

            var basePath = _options.Endpoints.BasePath;

            var tokenUrl = UAuthUrlBuilder.Build(basePath, _options.Endpoints.AntiforgeryToken, _options.MultiTenant);

            string? trustedOrigin = null;

            if (Uri.TryCreate(basePath, UriKind.Absolute, out var hubUri))
            {
                if (hubUri.Scheme != Uri.UriSchemeHttp && hubUri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidOperationException("UAuthHub URL must use HTTP or HTTPS.");
                }

                trustedOrigin = hubUri.GetLeftPart(UriPartial.Authority);
            }

            await _browser.SetDeviceIdAsync(deviceId.Value);

            await _browser.ConfigureAntiforgeryAsync(tokenUrl, trustedOrigin);

            _started = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
