using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class BrowserUAuthBridge : IBrowserUAuthBridge
{
    private readonly IJSRuntime _js;

    public BrowserUAuthBridge(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask SetDeviceIdAsync(string deviceId)
    {
        return _js.InvokeVoidAsync("uauth.setDeviceId", deviceId);
    }
}
