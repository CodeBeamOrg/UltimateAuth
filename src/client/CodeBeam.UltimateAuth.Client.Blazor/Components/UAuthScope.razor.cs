using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public partial class UAuthScope : UAuthComponentBase
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
