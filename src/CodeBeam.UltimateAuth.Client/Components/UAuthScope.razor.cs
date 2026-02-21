using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace CodeBeam.UltimateAuth.Client;

public partial class UAuthScope : UAuthReactiveComponentBase
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
