using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public abstract class UAuthReactiveComponentBase : ComponentBase, IDisposable
{
    private UAuthState? _previousState;
    private bool _rendered;

    [CascadingParameter]
    protected UAuthState AuthState { get; set; } = default!;

    [Inject] protected NavigationManager Nav { get; set; } = default!;

    /// <summary>
    /// Automatically re-render when UAuthState changes.
    /// Can be overridden to disable.
    /// </summary>
    protected virtual bool AutoRefreshOnAuthStateChanged => true;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (AuthState is null)
            throw new InvalidOperationException($"{GetType().Name} requires a cascading parameter of type {nameof(AuthState)}. " +
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        
        if (firstRender)
            _rendered = true;
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

    protected virtual void OnUnauthorized()
    {
        Nav.NavigateTo("/");
    }

    protected virtual void OnForbidden()
    {
        Nav.NavigateTo("/forbidden");
    }

    public virtual void Dispose()
    {
        if (_previousState is not null)
            _previousState.Changed -= OnAuthStateChanged;
    }
}