using CodeBeam.UltimateAuth.Client.Authentication;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Components;

public partial class UAuthAuthenticationState
{
    private bool _initialized;
    private UAuthState _uauthState;

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
        _uauthState = StateManager.State;

        StateManager.State.Changed += OnStateChanged;
    }

    private void OnStateChanged(UAuthStateChangeReason _)
    {
        //StateManager.EnsureAsync();
        if (_ == UAuthStateChangeReason.MarkedStale)
        {
            StateManager.EnsureAsync();
        }
        _uauthState = StateManager.State;
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        StateManager.State.Changed -= OnStateChanged;
    }
}
