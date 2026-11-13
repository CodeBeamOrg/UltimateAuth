namespace CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

/// <summary>
/// Coordinates expensive password hashing operations across
/// the shared budget and workload-specific limits.
/// </summary>
public interface IUAuthPasswordHashLimiter
{
    Task<T> ExecuteAsync<T>(
        UAuthPasswordHashCategory workload,
        Func<Task<T>> operation,
        CancellationToken ct = default);

    Task ExecuteAsync(
        UAuthPasswordHashCategory workload,
        Action operation,
        CancellationToken ct = default);
}
