using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client;

public partial class UAuthApp
{
    private bool _initialized;

    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        if (_initialized)
            return;

        _initialized = true;
        //await Bootstrapper.EnsureStartedAsync();
        await StateManager.EnsureAsync();

        StateManager.State.Changed += OnStateChanged;
    }

    private void OnStateChanged(UAuthStateChangeReason reason)
    {
        if (reason == UAuthStateChangeReason.MarkedStale)
        {
            _ = InvokeAsync(async () =>
            {
                await StateManager.EnsureAsync();
            });
        }

        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        StateManager.State.Changed -= OnStateChanged;
    }
}
