using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client;

public abstract class UAuthReactiveComponentBase : ComponentBase, IDisposable
{
    private UAuthState? _previousState;

    [CascadingParameter]
    protected UAuthState UAuth { get; set; } = default!;

    /// <summary>
    /// Automatically re-render when UAuthState changes.
    /// Can be overridden to disable.
    /// </summary>
    protected virtual bool AutoRefreshOnAuthStateChanged => true;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (UAuth is null)
            throw new InvalidOperationException($"{GetType().Name} requires a cascading parameter of type {nameof(UAuthState)}. " +
                $"Make sure it is used inside <UAuthApp>.");

        if (!ReferenceEquals(_previousState, UAuth))
        {
            if (_previousState is not null)
                _previousState.Changed -= OnAuthStateChanged;

            UAuth.Changed += OnAuthStateChanged;
            _previousState = UAuth;
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