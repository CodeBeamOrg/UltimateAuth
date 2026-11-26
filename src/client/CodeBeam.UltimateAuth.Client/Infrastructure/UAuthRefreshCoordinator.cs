using CodeBeam.UltimateAuth.Client.Abstractions;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class UAuthRefreshCoordinator : IUAuthRefreshCoordinator
{
    private readonly IUAuthRefreshExecutor _executor;
    private readonly object _sync = new();

    private Task<RefreshResult>? _inFlight;

    public UAuthRefreshCoordinator(IUAuthRefreshExecutor executor)
    {
        _executor = executor;
    }

    public Task<RefreshResult> RefreshAsync(UAuthRefreshTrigger trigger, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        Task<RefreshResult> task;

        lock (_sync)
        {
            if (_inFlight is { IsCompleted: false })
            {
                task = _inFlight;
            }
            else
            {
                task = ExecuteAsync(trigger);
                _inFlight = task;
            }
        }

        return task.WaitAsync(ct);
    }

    private async Task<RefreshResult> ExecuteAsync(UAuthRefreshTrigger trigger)
    {
        return await _executor.ExecuteAsync(trigger);
    }
}
