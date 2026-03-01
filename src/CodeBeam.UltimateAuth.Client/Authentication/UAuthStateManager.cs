using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Client.Authentication;

internal sealed class UAuthStateManager : IUAuthStateManager, IDisposable
{
    private readonly IUAuthClient _client;
    private readonly ISessionEvents _events;
    private readonly IClock _clock;

    public UAuthState State { get; } = UAuthState.Anonymous();

    public UAuthStateManager(IUAuthClient client, ISessionEvents events, IClock clock)
    {
        _client = client;
        _events = events;
        _clock = clock;

        _events.CurrentSessionRevoked += OnCurrentSessionRevoked;
    }

    public async Task EnsureAsync(bool force = false, CancellationToken ct = default)
    {
        if (!force && State.IsAuthenticated && !State.IsStale)
            return;

        var result = await _client.Flows.ValidateAsync();

        if (!result.IsValid || result.Snapshot == null)
        {
            if (State.IsAuthenticated)
            {
                State.MarkStale();
                return;
            }

            State.Clear();
            return;
        }

        State.ApplySnapshot(result.Snapshot, _clock.UtcNow);
    }

    public Task OnLoginAsync()
    {
        State.MarkStale();
        return Task.CompletedTask;
    }

    public Task OnLogoutAsync()
    {
        State.Clear();
        return Task.CompletedTask;
    }

    public void MarkStale()
    {
        State.MarkStale();
    }

    public void Clear()
    {
        State.Clear();
    }

    private void OnCurrentSessionRevoked()
    {
        if (State.IsAuthenticated)
            Clear();
    }

    public void Dispose()
    {
        _events.CurrentSessionRevoked -= OnCurrentSessionRevoked;
    }

    public bool NeedsValidation => !State.IsAuthenticated || State.IsStale;
}
