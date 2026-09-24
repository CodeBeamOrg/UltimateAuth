using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Base class for Blazor components that participate in UltimateAuth authentication state and authorization lifecycle.
/// </summary>
public abstract class UAuthComponentBase : ComponentBase, IDisposable
{
    private UAuthState? _previousState;
    private bool _rendered;

    /// <summary>
    /// Gets the current UltimateAuth authentication state supplied by <c>UAuthApp</c>.
    /// </summary>
    [CascadingParameter]
    protected UAuthState AuthState { get; set; } = default!;

    /// <summary>
    /// Gets the Blazor navigation service.
    /// </summary>
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Automatically re-render when UAuthState changes. Can be overridden to disable.
    /// </summary>
    protected virtual bool AutoRefreshOnAuthStateChanged => true;

    /// <summary>
    /// Called when the component's parameters have been set. This method ensures that the component is properly registered 
    /// with the current <see cref="UAuthState"/> and evaluates authorization requirements.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (AuthState is null)
            throw new InvalidOperationException($"{GetType().Name} requires a cascading parameter of type {nameof(UAuthState)}. " +
                $"Make sure it is used inside <UAuthApp>.");

        if (!ReferenceEquals(_previousState, AuthState))
        {
            if (_previousState is not null)
                _previousState.Changed -= OnAuthStateChanged;

            AuthState.Changed += OnAuthStateChanged;
            _previousState = AuthState;
        }

        EvaluateAuthorization();
    }

    /// <summary>
    /// Called after the component has been rendered. This method sets the _rendered flag to true on the first render.
    /// </summary>
    /// <param name="firstRender"></param>
    /// <returns></returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        
        if (firstRender)
        {
            _rendered = true;
            // Never call EvaluateAuthorization() here, because it breaks UAuthAuthorize attribute behavior.
        }
    }

    private void OnAuthStateChanged(UAuthStateChangeReason reason)
    {
        HandleAuthStateChanged(reason);

        EvaluateAuthorization();

        if (AutoRefreshOnAuthStateChanged)
            _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Allows derived components to react to auth state changes.
    /// </summary>
    protected virtual void HandleAuthStateChanged(UAuthStateChangeReason reason)
    {
    }

    private void EvaluateAuthorization()
    {
        var attr = GetType()
            .GetCustomAttributes(typeof(UAuthAuthorizeAttribute), true)
            .FirstOrDefault() as UAuthAuthorizeAttribute;

        if (attr is null)
            return;

        if (_rendered && !AuthState.IsAuthenticated)
        {
            OnUnauthorized();
            return;
        }

        if (_rendered && !string.IsNullOrEmpty(attr.Roles))
        {
            var roles = attr.Roles.Split(',');

            if (!roles.Any(r => AuthState.IsInRole(r.Trim())))
            {
                OnForbidden();
                return;
            }
        }

        if (_rendered && !string.IsNullOrEmpty(attr.Permissions))
        {
            var permissions = attr.Permissions.Split(',');

            if (!permissions.Any(p => AuthState.HasPermission(p.Trim())))
            {
                OnForbidden();
                return;
            }
        }
    }

    /// <summary>
    /// Called when the component requires authentication but the current user is not authenticated.
    /// </summary>
    protected virtual void OnUnauthorized()
    {
        Navigation.NavigateTo("/");
    }

    /// <summary>
    /// Called when the current user is authenticated but does not satisfy the authorization requirements of the component.
    /// </summary>
    protected virtual void OnForbidden()
    {
        Navigation.NavigateTo("/forbidden");
    }

    /// <summary>
    /// Disposes of the component and unsubscribes from the <see cref="UAuthState.Changed"/> event to prevent memory leaks.
    /// </summary>
    public virtual void Dispose()
    {
        if (_previousState is not null)
            _previousState.Changed -= OnAuthStateChanged;
    }
}