using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client;

public partial class UAuthStateView
{
    [CascadingParameter]
    public UAuthState UAuthState { get; set; } = default!;

    [Parameter]
    public RenderFragment<UAuthState>? Authorized { get; set; }

    [Parameter]
    public RenderFragment? NotAuthorized { get; set; }

    [Parameter]
    public RenderFragment<UAuthState>? Inactive { get; set; }

    [Parameter]
    public RenderFragment<UAuthState>? ChildContent { get; set; }

    [Parameter]
    public string? Roles { get; set; }

    [Parameter]
    public string? Policy { get; set; }

    [Parameter]
    public bool RequireActive { get; set; } = true;

    private bool _inactive;

    protected override void OnInitialized()
    {
        UAuthState.Changed += OnStateChanged;
    }

    private UAuthState? _previousState;

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_previousState, UAuthState))
        {
            if (_previousState is not null)
                _previousState.Changed -= OnStateChanged;

            UAuthState.Changed += OnStateChanged;
            _previousState = UAuthState;
        }

        EvaluateSessionState();
    }

    private void EvaluateSessionState()
    {
        if (!RequireActive)
        {
            _inactive = false;
            return;
        }

        if (!UAuthState.IsAuthenticated)
        {
            _inactive = false;
            return;
        }

        if (UAuthState.Identity?.SessionState is null)
        {
            _inactive = false;
        }
        else
        {
            _inactive = UAuthState.Identity.SessionState != SessionState.Active;
        }
    }

    private void OnStateChanged(UAuthStateChangeReason _)
    {
        EvaluateSessionState();
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        UAuthState.Changed -= OnStateChanged;
    }
}
