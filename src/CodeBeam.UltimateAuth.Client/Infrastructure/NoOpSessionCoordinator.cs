using CodeBeam.UltimateAuth.Client.Abstractions;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class NoOpSessionCoordinator : ISessionCoordinator
{
#pragma warning disable CS0067
    public event Action? ReauthRequired;
#pragma warning restore CS0067

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
