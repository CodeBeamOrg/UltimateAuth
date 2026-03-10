using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client;

public partial class UAuthApp
{
    private bool _initialized;
    private bool _coordinatorStarted;

    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    [Parameter]
    public UAuthRenderMode RenderMode { get; set; } = UAuthRenderMode.Manual;

    [Parameter]
    public EventCallback OnReauthRequired { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Coordinator.ReauthRequired += HandleReauthRequired;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (_initialized)
                return;

            _initialized = true;

            StateManager.State.RequestRender = () => InvokeAsync(StateHasChanged);

            await Bootstrapper.EnsureStartedAsync();
            await StateManager.EnsureAsync();

            if (StateManager.State.IsAuthenticated)
            {
                await Coordinator.StartAsync();
                _coordinatorStarted = true;
            }

            StateManager.State.Changed += OnStateChanged;

            StateHasChanged();
        }

        if (StateManager.State.NeedsValidation)
        {
            await StateManager.EnsureAsync(true);
        }
    }

    private void OnStateChanged(UAuthStateChangeReason reason)
    {
        if (reason == UAuthStateChangeReason.Authenticated)
        {
            _ = InvokeAsync(async () =>
            {
                await Coordinator.StartAsync();
            });
        }

        if (reason == UAuthStateChangeReason.Cleared)
        {
            _ = InvokeAsync(async () =>
            {
                await Coordinator.StopAsync();
            });
        }

        if (RenderMode == UAuthRenderMode.Reactive)
        {
            InvokeAsync(StateHasChanged);
        }
    }

    private async void HandleReauthRequired()
    {
        StateManager.MarkStale();
        if (OnReauthRequired.HasDelegate)
            await OnReauthRequired.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        StateManager.State.Changed -= OnStateChanged;
        Coordinator.ReauthRequired -= HandleReauthRequired;

        if (_coordinatorStarted)
            await Coordinator.StopAsync();
    }
}
