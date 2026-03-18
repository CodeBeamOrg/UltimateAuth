using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

internal sealed class EfCoreRefreshTokenStore : IRefreshTokenStore
{
    private readonly UltimateAuthTokenDbContext _db;
    private readonly TenantKey _tenant;
    private bool _inTransaction;

    public EfCoreRefreshTokenStore(UltimateAuthTokenDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant.Tenant;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            _inTransaction = true;

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
                _inTransaction = false;
            }
        });
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            _inTransaction = true;

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
                _inTransaction = false;
            }
        });
    }

    private void EnsureTransaction()
    {
        if (!_inTransaction)
            throw new InvalidOperationException("Operation must be executed inside ExecuteAsync.");
    }

    public Task StoreAsync(RefreshToken token, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        EnsureTransaction();

        if (token.Tenant != _tenant)
            throw new InvalidOperationException("Tenant mismatch.");

        _db.RefreshTokens.Add(token.ToProjection());

        return Task.CompletedTask;
    }

    public async Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var p = await _db.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Tenant == _tenant &&
                     x.TokenHash == tokenHash,
                ct);

        return p?.ToDomain();
    }

    public Task RevokeAsync(string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        EnsureTransaction();

        var query = _db.RefreshTokens
            .Where(x =>
                x.Tenant == _tenant &&
                x.TokenHash == tokenHash &&
                x.RevokedAt == null);

        if (replacedByTokenHash is null)
        {
            return query.ExecuteUpdateAsync(
                x => x.SetProperty(t => t.RevokedAt, revokedAt),
                ct);
        }

        return query.ExecuteUpdateAsync(
            x => x
                .SetProperty(t => t.RevokedAt, revokedAt)
                .SetProperty(t => t.ReplacedByTokenHash, replacedByTokenHash),
            ct);
    }

    public Task RevokeBySessionAsync(AuthSessionId sessionId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        EnsureTransaction();

        return _db.RefreshTokens
            .Where(x =>
                x.Tenant == _tenant &&
                x.SessionId == sessionId &&
                x.RevokedAt == null)
            .ExecuteUpdateAsync(
                x => x.SetProperty(t => t.RevokedAt, revokedAt),
                ct);
    }

    public Task RevokeByChainAsync(SessionChainId chainId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        EnsureTransaction();

        return _db.RefreshTokens
            .Where(x =>
                x.Tenant == _tenant &&
                x.ChainId == chainId &&
                x.RevokedAt == null)
            .ExecuteUpdateAsync(
                x => x.SetProperty(t => t.RevokedAt, revokedAt),
                ct);
    }

    public Task RevokeAllForUserAsync(UserKey userKey, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        EnsureTransaction();

        return _db.RefreshTokens
            .Where(x =>
                x.Tenant == _tenant &&
                x.UserKey == userKey &&
                x.RevokedAt == null)
            .ExecuteUpdateAsync(
                x => x.SetProperty(t => t.RevokedAt, revokedAt),
                ct);
    }
}
