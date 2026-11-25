using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client.Blazor;

internal sealed class WebClientDeviceProvider : IClientDeviceProvider
{
    private readonly IJSRuntime _js;
    private readonly IDeviceIdProvider _deviceIdProvider;
    private readonly IUserAgentParser _userAgentParser;

    public WebClientDeviceProvider(IJSRuntime js, IDeviceIdProvider deviceIdProvider, IUserAgentParser userAgentParser)
    {
        _js = js;
        _deviceIdProvider = deviceIdProvider;
        _userAgentParser = userAgentParser;
    }

    public async Task<DeviceContext> GetAsync()
    {
        var deviceId = await _deviceIdProvider.GetOrCreateAsync();

        ClientDeviceJsInfo jsInfo;

        try
        {
            jsInfo = await _js.InvokeAsync<ClientDeviceJsInfo>("uauth.getDeviceInfo");
        }
        catch (JSDisconnectedException ex)
        {
            throw new UAuthTransportException("The JavaScript runtime was disconnected.", ex);
        }
        catch (JSException ex)
        {
            throw new UAuthTransportException("Unable to retrieve browser device information.", ex);
        }

        var ua = jsInfo.UserAgent;
        var parsed = _userAgentParser.Parse(ua);

        return DeviceContext.Create(
            deviceId,
            deviceType: "web",
            platform: parsed.Platform,
            operatingSystem: parsed.OperatingSystem,
            browser: parsed.Browser,
            ipAddress: null
        );
    }

    private sealed class ClientDeviceJsInfo
    {
        public string UserAgent { get; set; } = default!;
        public string Platform { get; set; } = default!;
        public string Language { get; set; } = default!;
    }
}
