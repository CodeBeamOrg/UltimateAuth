using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client;

public partial class UAuthStateView : UAuthReactiveComponentBase
{
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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        EvaluateSessionState();
    }

    protected override void HandleAuthStateChanged(UAuthStateChangeReason reason)
    {
        EvaluateSessionState();
    }

    private void EvaluateSessionState()
    {
        if (!RequireActive)
        {
            _inactive = false;
            return;
        }

        if (UAuth.IsAuthenticated != true)
        {
            _inactive = false;
            return;
        }

        if (UAuth.Identity?.SessionState is null)
        {
            _inactive = false;
        }
        else
        {
            _inactive = UAuth.Identity?.SessionState != SessionState.Active;
        }
    }
}
