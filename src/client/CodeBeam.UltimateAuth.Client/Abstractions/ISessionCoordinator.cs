namespace CodeBeam.UltimateAuth.Client.Abstractions;

/// <summary>
/// Represents a coordinator for managing user sessions, providing methods to start and stop session coordination,
/// and an event to notify when reauthentication is required.
/// </summary>
public interface ISessionCoordinator : IAsyncDisposable
{
    /// <summary>
    /// Starts session coordination.
    /// Should be idempotent.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Restores authentication once at application startup when supported.</summary>
    Task RestoreAsync() => Task.CompletedTask;

    /// <summary>
    /// Stops coordination (optional).
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Event triggered when reauthentication is required.
    /// </summary>
    event Action? ReauthRequired;
}
