using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Provides the root Blazor integration component for UltimateAuth.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="UAuthApp"/> initializes the UltimateAuth client runtime,
/// exposes the current <see cref="UAuthState"/> as a cascading value,
/// coordinates the authenticated session lifecycle, and optionally provides
/// the application's Blazor router.
/// </para>
/// <para>
/// Applications using UltimateAuth Blazor components should normally place
/// their application content within this component.
/// </para>
/// <para>
/// Client-side authentication state is intended for UI behavior only and
/// does not constitute a security boundary. Authorization of protected
/// resources must always be enforced by the server.
/// </para>
/// </remarks>
public partial class UAuthApp
{
    private bool _initialized;
    private bool _coordinatorStarted;

    /// <summary>
    /// Gets or sets the application content rendered within the UltimateAuth context.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the content rendered by the built-in router when the current user is not authorized to access a route.
    /// </summary>
    /// <remarks>
    /// This parameter is used only when <see cref="UseBuiltInRouter"/> is enabled.
    /// </remarks>
    [Parameter]
    public RenderFragment? NotAuthorized { get; set; }

    /// <summary>
    /// Gets or sets whether <see cref="UAuthApp"/> should provide the application's Blazor router.
    /// </summary>
    /// <remarks>
    /// Set this to <see langword="false"/> when the application provides its own router.
    /// </remarks>
    [Parameter]
    public bool UseBuiltInRouter { get; set; }

    /// <summary>
    /// Gets or sets whether UltimateAuth client routes are included in the assemblies searched by the built-in router.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="true"/>.
    /// </remarks>
    [Parameter]
    public bool UseUAuthClientRoutes { get; set; } = true;

    /// <summary>
    /// Gets or sets the assembly containing the application's routable components.
    /// </summary>
    /// <remarks>
    /// This value is used by the built-in router.
    /// </remarks>
    [Parameter]
    public Assembly? AppAssembly { get; set; }

    /// <summary>
    /// Gets or sets additional assemblies that should be searched for routable components.
    /// </summary>
    /// <remarks>
    /// When <see cref="UseUAuthClientRoutes"/> is enabled, the UltimateAuth
    /// Blazor client assemblies are added to this set automatically.
    /// </remarks>
    [Parameter]
    public IEnumerable<Assembly>? AdditionalAssemblies { get; set; }

    /// <summary>
    /// Gets or sets the default layout used by the built-in
    /// <see cref="Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView"/>.
    /// </summary>
    [Parameter]
    public Type? DefaultLayout { get; set; }

    /// <summary>
    /// Gets or sets the CSS selector used by Blazor's focus-on-navigation behavior.
    /// </summary>
    /// <remarks>
    /// The default value is <c>h1</c>.
    /// </remarks>
    [Parameter]
    public string? FocusSelector { get; set; } = "h1";

    /// <summary>
    /// Gets or sets how UltimateAuth state changes affect component rendering.
    /// </summary>
    /// <remarks>
    /// The default value is <see cref="UAuthRenderMode.Manual"/>.
    /// </remarks>
    [Parameter]
    public UAuthRenderMode RenderMode { get; set; } = UAuthRenderMode.Manual;

    /// <summary>
    /// Gets or sets the callback invoked when the session coordinator determines that reauthentication is required.
    /// </summary>
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
                _coordinatorStarted = true;
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
            return UAuthAssemblies.BlazorClient();

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
