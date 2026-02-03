using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Authentication;

internal sealed class UAuthCascadingStateProvider : CascadingValueSource<UAuthState>, IDisposable
{
    private readonly IUAuthStateManager _stateManager;

    public UAuthCascadingStateProvider(IUAuthStateManager stateManager)
        : base(() => stateManager.State, isFixed: false)
    {
        _stateManager = stateManager;
        _stateManager.State.Changed += OnStateChanged;
    }

    private void OnStateChanged(UAuthStateChangeReason _)
    {
        NotifyChangedAsync();
    }

    public void Dispose()
    {
        _stateManager.State.Changed -= OnStateChanged;
    }
}
