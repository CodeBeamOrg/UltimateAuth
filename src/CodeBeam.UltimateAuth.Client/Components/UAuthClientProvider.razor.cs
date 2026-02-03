using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client;

// TODO: Add CircuitHandler to manage start/stop of coordinator in server-side Blazor
public partial class UAuthClientProvider : ComponentBase, IAsyncDisposable
{
    private bool _started;

    [Parameter]
    public EventCallback OnReauthRequired { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Coordinator.ReauthRequired += HandleReauthRequired;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _started)
            return;

        _started = true;
        // TODO: Add device id auto creation for MVC, this is only for blazor.
        var deviceId = await DeviceIdProvider.GetOrCreateAsync();
        await BrowserUAuthBridge.SetDeviceIdAsync(deviceId.Value);
        await Coordinator.StartAsync();
        StateHasChanged();
    }

    private async void HandleReauthRequired()
    {
        if (OnReauthRequired.HasDelegate)
            await OnReauthRequired.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Coordinator.StopAsync();
    }
}
