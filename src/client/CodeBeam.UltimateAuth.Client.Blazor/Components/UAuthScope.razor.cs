using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public partial class UAuthScope : UAuthReactiveComponentBase
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
