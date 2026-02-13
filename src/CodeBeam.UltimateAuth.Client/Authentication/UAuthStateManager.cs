using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Client.Authentication;

internal sealed class UAuthStateManager : IUAuthStateManager
{
    private readonly IUAuthClient _client;
    private readonly IClock _clock;
    private readonly IUAuthClientBootstrapper _bootstrapper;

    public UAuthState State { get; } = UAuthState.Anonymous();

    public UAuthStateManager(IUAuthClient client, IClock clock, IUAuthClientBootstrapper bootstrapper)
    {
        _client = client;
        _clock = clock;
        _bootstrapper = bootstrapper;
    }

    public async Task EnsureAsync(CancellationToken ct = default)
    {
        if (State.IsAuthenticated && !State.IsStale)
            return;

        await _bootstrapper.EnsureStartedAsync();
        var result = await _client.Flows.ValidateAsync();

        if (!result.IsValid || result.Snapshot == null)
        {
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
}
