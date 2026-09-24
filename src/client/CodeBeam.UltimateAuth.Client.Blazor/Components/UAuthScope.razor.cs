using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// A Blazor component that defines a scope for UltimateAuth authentication and authorization.
/// It can be used to group child components that require specific authentication or authorization context.
/// </summary>
public partial class UAuthScope : UAuthComponentBase
{
    /// <summary>
    /// Gets or sets the child content to be rendered within this scope.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
