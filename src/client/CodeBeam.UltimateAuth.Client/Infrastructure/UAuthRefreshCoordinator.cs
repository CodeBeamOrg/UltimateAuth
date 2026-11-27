using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class UAuthRefreshCoordinator : IUAuthRefreshCoordinator
{
    private readonly IUAuthRefreshExecutor _executor;
    private readonly IUAuthTokenStore _tokenStore;
    private readonly object _sync = new();

    private Task<RefreshResult>? _inFlight;

    public UAuthRefreshCoordinator(IUAuthRefreshExecutor executor, IUAuthTokenStore tokenStore)
    {
        _executor = executor;
        _tokenStore = tokenStore;
    }

    public Task<RefreshResult> RefreshAsync(UAuthRefreshTrigger trigger, CancellationToken ct = default)
    {
        return RunAsync(trigger, expected: null, ct);
    }

    public Task<RefreshResult> RefreshIfCurrentAsync(UAuthRefreshTrigger trigger, UAuthTokenSnapshot expected, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        return RunAsync(trigger, expected, ct);
    }

    private Task<RefreshResult> RunAsync(UAuthRefreshTrigger trigger, UAuthTokenSnapshot? expected, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Task<RefreshResult> task;

        lock (_sync)
        {
            if (_inFlight is { IsCompleted: false })
            {
                task = _inFlight;
            }
            else if (expected is not null && !IsCurrent(expected))
            {
                task = Task.FromResult(new RefreshResult
                {
                    IsSuccess = false,
                    Outcome = RefreshOutcome.None,
                    ErrorCode = "token_changed"
                });
            }
            else
            {
                task = ExecuteAsync(trigger);
                _inFlight = task;
            }
        }

        return task.WaitAsync(ct);
    }

    private bool IsCurrent(UAuthTokenSnapshot expected)
    {
        var current = _tokenStore.Capture();

        return current.Epoch == expected.Epoch && current.Generation == expected.Generation;
    }

    private async Task<RefreshResult> ExecuteAsync(UAuthRefreshTrigger trigger)
    {
        return await _executor.ExecuteAsync(trigger);
    }
}
