using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Base class for Blazor components that participate in the UltimateAuth authentication state lifecycle.
/// </summary>
public abstract class UAuthComponentBase : ComponentBase, IDisposable
{
    private UAuthState? _subscribedState;
    private bool _disposed;

    /// <summary>
    /// Gets the current UltimateAuth authentication state
    /// supplied by UAuthApp.
    /// </summary>
    [CascadingParameter]
    protected UAuthState AuthState { get; set; } = default!;

    /// <summary>
    /// Gets the Blazor navigation service.
    /// </summary>
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Determines whether authentication state changes
    /// automatically trigger a component render.
    /// </summary>
    protected virtual bool AutoRefreshOnAuthStateChanged => true;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (AuthState is null)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} requires a cascading parameter " +
                $"of type {nameof(UAuthState)}. " +
                "Make sure it is used inside <UAuthApp>.");
        }

        if (ReferenceEquals(_subscribedState, AuthState))
            return;

        Unsubscribe();

        AuthState.Changed += OnAuthStateChanged;
        _subscribedState = AuthState;
    }

    private void OnAuthStateChanged(UAuthStateChangeReason reason)
    {
        if (_disposed)
            return;

        _ = InvokeAsync(() =>
        {
            if (_disposed)
                return;

            HandleAuthStateChanged(reason);

            if (AutoRefreshOnAuthStateChanged)
                StateHasChanged();
        });
    }

    /// <summary>
    /// Called when the UltimateAuth authentication state changes.
    /// </summary>
    protected virtual void HandleAuthStateChanged(UAuthStateChangeReason reason)
    {
    }

    private void Unsubscribe()
    {
        if (_subscribedState is null)
            return;

        _subscribedState.Changed -= OnAuthStateChanged;
        _subscribedState = null;
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Unsubscribe();
    }
}
