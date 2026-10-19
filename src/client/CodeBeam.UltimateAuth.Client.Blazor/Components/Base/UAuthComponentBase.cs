using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Base class for Blazor components that participate in
/// the UltimateAuth authentication state lifecycle.
/// </summary>
public abstract class UAuthComponentBase : ComponentBase, IDisposable
{
    private UAuthState? _subscribedState;
    private bool _disposed;

    [CascadingParameter]
    protected UAuthState AuthState { get; set; } = default!;

    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Automatically re-render when the authentication state changes.
    /// </summary>
    protected virtual bool AutoRefreshOnAuthStateChanged => true;

    /// <summary>
    /// Indicates whether the component has been disposed.
    /// </summary>
    protected bool IsDisposed => _disposed;

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

        _subscribedState = AuthState;
        _subscribedState.Changed += OnAuthStateChanged;
    }

    private void OnAuthStateChanged(UAuthStateChangeReason reason)
    {
        if (_disposed)
            return;

        _ = DispatchAuthStateChangedAsync(reason);
    }

    private async Task DispatchAuthStateChangedAsync(UAuthStateChangeReason reason)
    {
        try
        {
            await InvokeAsync(async () =>
            {
                if (_disposed)
                    return;

                await HandleAuthStateChangedAsync(reason);

                if (!_disposed && AutoRefreshOnAuthStateChanged)
                    StateHasChanged();
            });
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            // Ignore disposal-related exceptions.
        }
        catch (Exception ex)
        {
            if (!_disposed)
                await DispatchExceptionAsync(ex);
        }
    }

    /// <summary>
    /// Called when the authentication state changes.
    /// Override to perform asynchronous state-dependent work.
    /// </summary>
    protected virtual Task HandleAuthStateChangedAsync(UAuthStateChangeReason reason)
    {
        return Task.CompletedTask;
    }

    private void Unsubscribe()
    {
        if (_subscribedState is null)
            return;

        _subscribedState.Changed -= OnAuthStateChanged;
        _subscribedState = null;
    }

    public virtual void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Unsubscribe();
    }
}
