using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

internal sealed class EfCoreUAuthAtomicExecutor<TDbContext> : IUAuthAtomicExecutor where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfCoreUAuthAtomicExecutor(TDbContext db)
    {
        _db = db;
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

        // Participate in an already active transaction.
        if (_db.Database.CurrentTransaction is not null)
        {
            return await operation(ct);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var result = await operation(ct);

            await transaction.CommitAsync(ct);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);

            throw;
        }
    }
}
