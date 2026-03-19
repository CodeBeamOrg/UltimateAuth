using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

public abstract class UAuthReactiveComponentBase : ComponentBase, IDisposable
{
    private UAuthState? _previousState;

    [CascadingParameter]
    protected UAuthState AuthState { get; set; } = default!;

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
    }

    private void OnAuthStateChanged(UAuthStateChangeReason reason)
    {
        HandleAuthStateChanged(reason);

        if (AutoRefreshOnAuthStateChanged)
            _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Allows derived components to react to auth state changes.
    /// </summary>
    protected virtual void HandleAuthStateChanged(UAuthStateChangeReason reason)
    {
    }

    public virtual void Dispose()
    {
        if (_previousState is not null)
            _previousState.Changed -= OnAuthStateChanged;
    }
}