using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal sealed class EfCoreSessionStoreKernel<TUserId>
    {
        private readonly UltimateAuthSessionDbContext<TUserId> _db;

        public EfCoreSessionStoreKernel(UltimateAuthSessionDbContext<TUserId> db)
        {
            _db = db;
        }

        public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                var connection = _db.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(ct);

                await using var tx = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
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
            });
        }

    }
}
