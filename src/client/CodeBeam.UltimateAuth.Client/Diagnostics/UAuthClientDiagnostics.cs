using CodeBeam.UltimateAuth.Client.Contracts;

namespace CodeBeam.UltimateAuth.Client.Diagnostics;

/// <summary>
/// Represents diagnostic information for the UAuth client, tracking its lifecycle events and refresh attempts.
/// </summary>
public sealed class UAuthClientDiagnostics
{
    private int _terminatedCount;

    /// <summary>
    /// Occurs when any diagnostic information changes, allowing subscribers to react to updates in the client's state.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the timestamp when the client was started, or null if it has not been started yet.
    /// </summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the client was stopped, or null if it has not been stopped yet.
    /// </summary>
    public DateTimeOffset? StoppedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the client was terminated, or null if it has not been terminated yet.
    /// </summary>
    public DateTimeOffset? TerminatedAt { get; private set; }


    /// <summary>
    /// Gets a value indicating whether the client is currently running, which is true if it has been started and has not been stopped or terminated.
    /// </summary>
    public bool IsRunning => StartedAt is not null && !IsStopped && !IsTerminated;

    /// <summary>
    /// Gets a value indicating whether the client has been stopped, which is true if it has a non-null StoppedAt timestamp.
    /// </summary>
    public bool IsStopped => StoppedAt is not null;

    /// <summary>
    /// Gets a value indicating whether the client has been terminated, which is true if it has a non-null TerminatedAt timestamp.
    /// </summary>
    public bool IsTerminated { get; private set; }


    /// <summary>
    /// Gets the reason for the client's termination, or null if it has not been terminated.
    /// This provides context for why the client was terminated, such as due to an error or a manual stop request.
    /// </summary>
    public CoordinatorTerminationReason? TerminationReason { get; private set; }

    /// <summary>
    /// Gets the total number of times the client has been terminated, which is incremented each time the MarkTerminated method is called.
    /// </summary>
    public int TerminatedCount => _terminatedCount;


    /// <summary>
    /// Gets the total number of times the client has been started and stopped, which are incremented each time the MarkStarted and MarkStopped methods are called, respectively.
    /// </summary>
    public int StartCount { get; private set; }

    /// <summary>
    /// Gets the total number of times the client has been stopped, which is incremented each time the MarkStopped method is called.
    /// </summary>
    public int StopCount { get; private set; }


    /// <summary>
    /// Gets the total number of refresh attempts made by the client, which is incremented each time either the MarkManualRefresh or MarkAutomaticRefresh methods are called.
    /// </summary>
    public int RefreshAttemptCount { get; private set; }

    /// <summary>
    /// Gets the total number of manual refresh attempts made by the client, which is incremented each time the MarkManualRefresh method is called.
    /// </summary>
    public int ManualRefreshCount { get; private set; }

    /// <summary>
    /// Gets the total number of automatic refresh attempts made by the client, which is incremented each time the MarkAutomaticRefresh method is called.
    /// </summary>
    public int AutomaticRefreshCount { get; private set; }


    /// <summary>
    /// Gets the total number of refresh attempts that resulted in a "touched" state, which is incremented each time the MarkRefreshTouched method is called.
    /// </summary>
    public int RefreshTouchedCount { get; private set; }

    /// <summary>
    /// Gets the total number of refresh attempts that resulted in a "rotated" state, which is incremented each time the MarkRefreshRotated method is called.
    /// </summary>
    public int RefreshRotatedCount { get; private set; }

    /// <summary>
    /// Gets the total number of refresh attempts that resulted in a "no operation" state, which is incremented each time the MarkRefreshNoOp method is called.
    /// </summary>
    public int RefreshNoOpCount { get; private set; }

    /// <summary>
    /// Gets the total number of refresh attempts that required reauthentication, which is incremented each time the MarkRefreshReauthRequired method is called.
    /// </summary>
    public int RefreshReauthRequiredCount { get; private set; }

    /// <summary>
    /// Gets the total number of successful refresh attempts, which is incremented each time the MarkRefreshSuccess method is called.
    /// </summary>
    public int RefreshSuccessCount { get; private set; }

    /// <summary>
    /// Gets the total duration for which the client has been running, calculated as the difference between the StartedAt timestamp and either the StoppedAt or TerminatedAt timestamp, or the current time if the client is still running. Returns null if the client has not been started yet.
    /// </summary>
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

    internal void MarkRefreshRotated()
    {
        RefreshRotatedCount++;
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
