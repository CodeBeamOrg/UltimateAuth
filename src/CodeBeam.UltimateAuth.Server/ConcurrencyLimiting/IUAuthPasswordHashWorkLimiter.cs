namespace CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

/// <summary>
/// Coordinates resource-intensive password hashing operations.
/// </summary>
public interface IUAuthPasswordHashWorkLimiter
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
}
