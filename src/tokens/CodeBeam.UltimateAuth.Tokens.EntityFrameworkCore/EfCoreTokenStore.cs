using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

internal sealed class EfCoreRefreshTokenStore : IRefreshTokenStore
{
    private readonly UltimateAuthTokenDbContext _db;

    public EfCoreRefreshTokenStore(UltimateAuthTokenDbContext db, IUserIdConverterResolver converters)
    {
        _db = db;
    }

    public async Task StoreAsync(string? tenantId, StoredRefreshToken token, CancellationToken ct = default)
    {
        if (token.TenantId != tenantId)
            throw new InvalidOperationException("TenantId mismatch between context and token.");

        _db.RefreshTokens.Add(new RefreshTokenProjection
        {
            TenantId = tenantId,
            TokenHash = token.TokenHash,
            UserKey = token.UserKey,
            SessionId = token.SessionId,
            ChainId = token.ChainId.Value,
            IssuedAt = token.IssuedAt,
            ExpiresAt = token.ExpiresAt
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<StoredRefreshToken?> FindByHashAsync(string? tenantId, string tokenHash, CancellationToken ct = default)
    {
        var e = await _db.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash &&
                     x.TenantId == tenantId,
                ct);

        if (e is null)
            return null;

        return new StoredRefreshToken
        {
            TenantId = e.TenantId,
            TokenHash = e.TokenHash,
            UserKey = e.UserKey,
            SessionId = e.SessionId,
            ChainId = e.ChainId,
            IssuedAt = e.IssuedAt,
            ExpiresAt = e.ExpiresAt,
            RevokedAt = e.RevokedAt
        };
    }

    public Task RevokeAsync(string? tenantId, string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken ct = default)
    {
        var query = _db.RefreshTokens
            .Where(x =>
                x.TokenHash == tokenHash &&
                x.TenantId == tenantId &&
                x.RevokedAt == null);

        if (replacedByTokenHash == null)
        {
            return query.ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, revokedAt), ct);
        }

        return query.ExecuteUpdateAsync(
            x => x
                .SetProperty(t => t.RevokedAt, revokedAt)
                .SetProperty(t => t.ReplacedByTokenHash, replacedByTokenHash),
            ct);
    }

    public Task RevokeBySessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset revokedAt, CancellationToken ct = default)
        => _db.RefreshTokens
            .Where(x =>
                x.TenantId == tenantId &&
                x.SessionId == sessionId.Value &&
                x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, revokedAt), ct);

    public Task RevokeByChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset revokedAt, CancellationToken ct = default)
        => _db.RefreshTokens
            .Where(x =>
                x.TenantId == tenantId &&
                x.ChainId == chainId &&
                x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, revokedAt), ct);

    public Task RevokeAllForUserAsync(string? tenantId, UserKey userKey, DateTimeOffset revokedAt, CancellationToken ct = default)
    {

        return _db.RefreshTokens
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserKey == userKey &&
                x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, revokedAt), ct);
    }
}
