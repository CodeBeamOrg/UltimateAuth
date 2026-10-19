using CodeBeam.UltimateAuth.Core.Contracts;
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
    private bool _evaluating;
    private long _evaluationVersion;
    private bool _disposed;

    [Inject]
    private IUAuthAuthorizationEvaluator AuthorizationEvaluator { get; set; } = default!;

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

    private UAuthState AuthState => CurrentAuthState ?? throw new InvalidOperationException($"{nameof(UAuthAuthorizeRouteView)} requires " + $"a cascading {nameof(UAuthState)}.");

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        ArgumentNullException.ThrowIfNull(RouteData);

        _requirement = RouteData.PageType
            .GetCustomAttribute<UAuthAuthorizeAttribute>(inherit: true);

        _hasUAuthRequirement = _requirement is not null;
        _runtimeReady = RuntimeReady;

        if (!_hasUAuthRequirement)
        {
            ++_evaluationVersion;

            _uauthAuthorized = true;
            _evaluating = false;

            Unsubscribe();
            return;
        }

        Subscribe(AuthState);

        await EvaluateAuthorizationAsync();
    }

    private async Task EvaluateAuthorizationAsync()
    {
        var version = ++_evaluationVersion;

        _uauthAuthorized = false;
        _evaluating = false;

        if (_disposed || !_runtimeReady || _requirement is null)
            return;

        var requirement = CreateRequirement(_requirement);

        var state = AuthState;

        _evaluating = true;

        try
        {
            var result = await AuthorizationEvaluator.EvaluateAsync(state, requirement);

            if (_disposed || version != _evaluationVersion)
                return;

            _uauthAuthorized = result.IsAuthorized;
        }
        finally
        {
            if (!_disposed && version == _evaluationVersion)
            {
                _evaluating = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private UAuthAuthorizationRequirement CreateRequirement(UAuthAuthorizeAttribute attribute)
    {
        return new UAuthAuthorizationRequirement
        {
            Roles = attribute.Roles,
            Permissions = attribute.Permissions,
            MatchMode = attribute.MatchMode,
            RequireActive = attribute.RequireActive
        };
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

        _ = InvokeAsync(async () =>
        {
            if (_disposed)
                return;

            await EvaluateAuthorizationAsync();

            if (!_disposed)
                StateHasChanged();
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ++_evaluationVersion;

        Unsubscribe();
    }
}
