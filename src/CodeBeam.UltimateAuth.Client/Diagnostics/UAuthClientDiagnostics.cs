using CodeBeam.UltimateAuth.Client.Contracts;

namespace CodeBeam.UltimateAuth.Client.Diagnostics;

public sealed class UAuthClientDiagnostics
{
    private int _terminatedCount;

    public event Action? Changed;

    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? StoppedAt { get; private set; }
    public DateTimeOffset? TerminatedAt { get; private set; }

    public bool IsRunning => StartedAt is not null && !IsStopped && !IsTerminated;
    public bool IsStopped => StoppedAt is not null;
    public bool IsTerminated { get; private set; }

    public CoordinatorTerminationReason? TerminationReason { get; private set; }
    public int TerminatedCount => _terminatedCount;

    public int StartCount { get; private set; }
    public int StopCount { get; private set; }

    public int RefreshAttemptCount { get; private set; }
    public int ManualRefreshCount { get; private set; }
    public int AutomaticRefreshCount { get; private set; }
   
    public int RefreshTouchedCount { get; private set; }
    public int RefreshNoOpCount { get; private set; }
    public int RefreshReauthRequiredCount { get; private set; }
    public int RefreshSuccessCount { get; private set; }

    public TimeSpan? RunningDuration =>
        StartedAt is null
            ? null
            : (IsStopped || IsTerminated
                ? (StoppedAt ?? TerminatedAt) - StartedAt
                : DateTimeOffset.UtcNow - StartedAt);

    internal void MarkStarted()
    {
        StartedAt = DateTimeOffset.UtcNow;
        StoppedAt = null;
        IsTerminated = false;
        TerminationReason = null;

        StartCount++;
        Changed?.Invoke();
    }

    internal void MarkStopped()
    {
        StoppedAt = DateTimeOffset.UtcNow;
        StopCount++;
        Changed?.Invoke();
    }

    internal void MarkManualRefresh()
    {
        RefreshAttemptCount++;
        ManualRefreshCount++;
        Changed?.Invoke();
    }

    internal void MarkAutomaticRefresh()
    {
        RefreshAttemptCount++;
        AutomaticRefreshCount++;
        Changed?.Invoke();
    }
    internal void MarkRefreshTouched()
    {
        RefreshTouchedCount++;
        Changed?.Invoke();
    }

    internal void MarkRefreshNoOp()
    {
        RefreshNoOpCount++;
        Changed?.Invoke();
    }

    internal void MarkRefreshReauthRequired()
    {
        RefreshReauthRequiredCount++;
        Changed?.Invoke();
    }

    internal void MarkRefreshSuccess()
    {
        RefreshSuccessCount++;
        Changed?.Invoke();
    }

    internal void MarkTerminated(CoordinatorTerminationReason reason)
    {
        IsTerminated = true;
        TerminatedAt = DateTimeOffset.UtcNow;
        TerminationReason = reason;
        Interlocked.Increment(ref _terminatedCount);
        Changed?.Invoke();
    }
}
