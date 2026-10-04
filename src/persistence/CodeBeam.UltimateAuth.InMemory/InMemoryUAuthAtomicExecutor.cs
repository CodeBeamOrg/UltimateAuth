using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.InMemory;

internal sealed class InMemoryUAuthAtomicExecutor : IUAuthAtomicExecutor
{
    private readonly InMemoryAtomicCoordinator _coordinator;
    private readonly InMemoryAtomicContextAccessor _contextAccessor;

    public InMemoryUAuthAtomicExecutor(InMemoryAtomicCoordinator coordinator, InMemoryAtomicContextAccessor contextAccessor)
    {
        _coordinator = coordinator;
        _contextAccessor = contextAccessor;
    }

    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return ExecuteAsync<object?>(
            async innerCt =>
            {
                await operation(innerCt);
                return null;
            },
            ct);
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Nested UltimateAuth operation participates in the
        // currently active atomic operation.
        if (_contextAccessor.Current is not null)
        {
            return await operation(ct);
        }

        await _coordinator.Gate.WaitAsync(ct);

        var context = new InMemoryAtomicContext();
        _contextAccessor.Current = context;

        try
        {
            return await operation(ct);
        }
        catch
        {
            context.Rollback();
            throw;
        }
        finally
        {
            _contextAccessor.Current = null;
            _coordinator.Gate.Release();
        }
    }
}
