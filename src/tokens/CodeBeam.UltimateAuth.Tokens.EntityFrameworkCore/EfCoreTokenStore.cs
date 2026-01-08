using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

internal sealed class EfCoreRefreshTokenStore<TUserId> : IRefreshTokenStore<TUserId>
{
    private readonly UltimateAuthTokenDbContext _db;
    private readonly IUserIdConverter<TUserId> _converter;

    public EfCoreRefreshTokenStore(
        UltimateAuthTokenDbContext db,
        IUserIdConverterResolver converters)
    {
        _db = db;
        _converter = converters.GetConverter<TUserId>();
    }

    public async Task StoreAsync(
        string? tenantId,
        StoredRefreshToken<TUserId> token,
        CancellationToken ct = default)
    {
        if (token.TenantId != tenantId)
            throw new InvalidOperationException("TenantId mismatch between context and token.");

        _db.RefreshTokens.Add(new RefreshTokenProjection
        {
            TenantId = tenantId,
            TokenHash = token.TokenHash,
            UserId = _converter.ToString(token.UserId),
            SessionId = token.SessionId.Value,
            ChainId = token.ChainId.Value,
            IssuedAt = token.IssuedAt,
            ExpiresAt = token.ExpiresAt
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<StoredRefreshToken<TUserId>?> FindByHashAsync(
    string? tenantId,
    string tokenHash,
    CancellationToken ct = default)
    {
        var e = await _db.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash &&
                     x.TenantId == tenantId,
                ct);

        if (e is null)
            return null;

        return new StoredRefreshToken<TUserId>
        {
            TenantId = e.TenantId,
            TokenHash = e.TokenHash,
            UserId = _converter.FromString(e.UserId),
            SessionId = new AuthSessionId(e.SessionId),
            ChainId = new ChainId(e.ChainId),
            IssuedAt = e.IssuedAt,
            ExpiresAt = e.ExpiresAt,
            RevokedAt = e.RevokedAt
        };
    }

    public Task RevokeAsync(
    string? tenantId,
    string tokenHash,
    DateTimeOffset revokedAt,
    CancellationToken ct = default)
    => _db.RefreshTokens
        .Where(x =>
            x.TokenHash == tokenHash &&
            x.TenantId == tenantId &&
            x.RevokedAt == null)
        .ExecuteUpdateAsync(
            x => x.SetProperty(t => t.RevokedAt, revokedAt),
            ct);

    public Task RevokeBySessionAsync(
        string? tenantId,
        AuthSessionId sessionId,
        DateTimeOffset revokedAt,
        CancellationToken ct = default)
        => _db.RefreshTokens
            .Where(x =>
                x.TenantId == tenantId &&
                x.SessionId == sessionId.Value &&
                x.RevokedAt == null)
            .ExecuteUpdateAsync(
                x => x.SetProperty(t => t.RevokedAt, revokedAt),
                ct);

    public Task RevokeByChainAsync(
        string? tenantId,
        ChainId chainId,
        DateTimeOffset revokedAt,
        CancellationToken ct = default)
        => _db.RefreshTokens
            .Where(x =>
                x.TenantId == tenantId &&
                x.ChainId == chainId.Value &&
                x.RevokedAt == null)
            .ExecuteUpdateAsync(
                x => x.SetProperty(t => t.RevokedAt, revokedAt),
                ct);

    public Task RevokeAllForUserAsync(
        string? tenantId,
        TUserId userId,
        DateTimeOffset revokedAt,
        CancellationToken ct = default)
    {
        var uid = _converter.ToString(userId);

        return _db.RefreshTokens
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == uid &&
                x.RevokedAt == null)
            .ExecuteUpdateAsync(
                x => x.SetProperty(t => t.RevokedAt, revokedAt),
                ct);
    }
}
