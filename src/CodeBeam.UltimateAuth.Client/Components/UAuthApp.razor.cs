using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace CodeBeam.UltimateAuth.Client;

public partial class UAuthApp
{
    private bool _initialized;
    private bool _coordinatorStarted;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? NotAuthorized { get; set; }

    [Parameter]
    public bool UseBuiltInRouter { get; set; }

    [Parameter]
    public bool UseUAuthClientRoutes { get; set; } = true;

    [Parameter]
    public Assembly? AppAssembly { get; set; }

    [Parameter]
    public IEnumerable<Assembly>? AdditionalAssemblies { get; set; }

    [Parameter]
    public Type? DefaultLayout { get; set; }

    [Parameter]
    public string? FocusSelector { get; set; } = "h1";

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

    private IEnumerable<Assembly> GetAdditionalAssemblies()
    {
        if (AdditionalAssemblies is null && UseUAuthClientRoutes)
            return UAuthAssemblies.Client();

        if (UseUAuthClientRoutes)
            return AdditionalAssemblies.WithUltimateAuth();

        return Enumerable.Empty<Assembly>();
    }

    public async ValueTask DisposeAsync()
    {
        StateManager.State.Changed -= OnStateChanged;
        Coordinator.ReauthRequired -= HandleReauthRequired;

        if (_coordinatorStarted)
            await Coordinator.StopAsync();
    }
}
