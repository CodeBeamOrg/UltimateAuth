namespace CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

/// <summary>
/// Coordinates expensive password hashing operations across
/// the shared budget and workload-specific limits.
/// </summary>
public interface IUAuthPasswordHashWorkLimiter
{
    Task<T> ExecuteAsync<T>(
        UAuthPasswordHashWorkload workload,
        Func<Task<T>> operation,
        CancellationToken ct = default);

    Task ExecuteAsync(
        UAuthPasswordHashWorkload workload,
        Action operation,
        CancellationToken ct = default);
}
