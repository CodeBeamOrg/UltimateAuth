using CodeBeam.UltimateAuth.Client.Abstractions;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class NoOpSessionCoordinator : ISessionCoordinator
{
    public event Action? ReauthRequired;

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
