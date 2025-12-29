using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

internal sealed class EfCoreTokenStoreKernel
{
    private readonly UltimateAuthTokenDbContext _db;

    public EfCoreTokenStoreKernel(UltimateAuthTokenDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);

        _db.Database.UseTransaction(tx);

        try
        {
            await action(ct);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            _db.Database.UseTransaction(null);
        }
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action,CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        _db.Database.UseTransaction(tx);

        try
        {
            var result = await action(ct);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            _db.Database.UseTransaction(null);
        }
    }
}
