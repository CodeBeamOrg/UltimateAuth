using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Reflection;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Renders a routed Blazor page using both UltimateAuth and ASP.NET Core authorization requirements.
/// </summary>
public partial class UAuthAuthorizeRouteView : ComponentBase, IDisposable
{
    private UAuthState? _subscribedState;
    private UAuthAuthorizeAttribute? _requirement;

    private bool _hasUAuthRequirement;
    private bool _uauthAuthorized;
    private bool _runtimeReady;
    private bool _disposed;

    /// <summary>
    /// The route to render.
    /// </summary>
    [Parameter, EditorRequired]
    public RouteData RouteData { get; set; } = default!;

    /// <summary>
    /// The default layout for the routed page.
    /// </summary>
    [Parameter]
    public Type? DefaultLayout { get; set; }

    /// <summary>
    /// Optional resource passed to ASP.NET Core authorization.
    /// </summary>
    [Parameter]
    public object? Resource { get; set; }

    /// <summary>
    /// Content displayed when authorization fails.
    /// </summary>
    [Parameter]
    public RenderFragment<AuthenticationState>? NotAuthorized { get; set; }

    /// <summary>
    /// Content displayed while authorization is pending.
    /// </summary>
    [Parameter]
    public RenderFragment? Authorizing { get; set; }

    /// <summary>
    /// The current UltimateAuth state.
    /// </summary>
    [CascadingParameter]
    private UAuthState? CurrentAuthState { get; set; }

    /// <summary>
    /// Indicates whether UltimateAuth runtime initialization
    /// has completed.
    /// </summary>
    [CascadingParameter(Name = "UAuthRuntimeReady")]
    private bool RuntimeReady { get; set; }

    private UAuthState AuthState => CurrentAuthState
        ?? throw new InvalidOperationException($"{nameof(UAuthAuthorizeRouteView)} requires " + $"a cascading {nameof(UAuthState)}.");

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ArgumentNullException.ThrowIfNull(RouteData);

        _requirement = RouteData.PageType.GetCustomAttribute<UAuthAuthorizeAttribute>(inherit: true);

        _hasUAuthRequirement = _requirement is not null;

        // Public routes remain under the standard
        // ASP.NET Core AuthorizeRouteView.
        if (!_hasUAuthRequirement)
        {
            _uauthAuthorized = true;
            Unsubscribe();
            return;
        }

        Subscribe(AuthState);

        _runtimeReady = RuntimeReady;

        EvaluateAuthorization();
    }

    private void EvaluateAuthorization()
    {
        // Fail closed until UltimateAuth initialization
        // has completed.
        _uauthAuthorized = false;

        if (!_runtimeReady || _requirement is null)
            return;

        if (!AuthState.IsAuthenticated)
            return;

        var roles = ParseValues(_requirement.Roles);
        var permissions = ParseValues(_requirement.Permissions);

        // Category semantics:
        // At least one role AND at least one permission,
        // when both categories are configured.
        if (roles.Length > 0 && !roles.Any(AuthState.IsInRole))
        {
            return;
        }

        if (permissions.Length > 0 && !permissions.Any(AuthState.HasPermission))
        {
            return;
        }

        _uauthAuthorized = true;
    }

    private static string[] ParseValues(string? values)
    {
        if (string.IsNullOrWhiteSpace(values))
            return [];

        return values
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    private void Subscribe(UAuthState state)
    {
        if (ReferenceEquals(_subscribedState, state))
            return;

        Unsubscribe();

        _subscribedState = state;
        _subscribedState.Changed += OnAuthStateChanged;
    }

    private void Unsubscribe()
    {
        if (_subscribedState is null)
            return;

        _subscribedState.Changed -= OnAuthStateChanged;
        _subscribedState = null;
    }

    private void OnAuthStateChanged(UAuthStateChangeReason reason)
    {
        if (_disposed)
            return;

        _ = InvokeAsync(() =>
        {
            if (_disposed)
                return;

            EvaluateAuthorization();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        _disposed = true;
        Unsubscribe();
    }
}
