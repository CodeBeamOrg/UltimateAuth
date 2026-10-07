namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IUAuthAtomicExecutor
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);

    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken ct = default);
}
