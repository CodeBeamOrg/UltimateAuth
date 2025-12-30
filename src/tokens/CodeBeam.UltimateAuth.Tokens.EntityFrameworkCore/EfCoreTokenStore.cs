using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

internal sealed class EfCoreTokenStore<TUserId> : ITokenStore<TUserId>
{
    private readonly UltimateAuthTokenDbContext _db;
    private readonly EfCoreTokenStoreKernel _kernel;
    private readonly ISessionStore<TUserId> _sessions;
    private readonly ITokenHasher _hasher;

    public EfCoreTokenStore(
        UltimateAuthTokenDbContext db,
        EfCoreTokenStoreKernel kernel,
        ISessionStore<TUserId> sessions,
        ITokenHasher hasher)
    {
        _db = db;
        _kernel = kernel;
        _sessions = sessions;
        _hasher = hasher;
    }

    public Task StoreRefreshTokenAsync(string? tenantId, TUserId userId, AuthSessionId sessionId, string refreshTokenHash, DateTimeOffset expiresAt)
    {
        return _kernel.ExecuteAsync(ct =>
        {
            _db.RefreshTokens.Add(new RefreshTokenProjection
            {
                TenantId = tenantId,
                TokenHash = refreshTokenHash,
                SessionId = sessionId,
                ExpiresAt = expiresAt
            });

            return Task.CompletedTask;
        });
    }

    public async Task<RefreshTokenValidationResult<TUserId>> ValidateRefreshTokenAsync(string? tenantId, string providedRefreshToken, DateTimeOffset now)
    {
        var hash = _hasher.Hash(providedRefreshToken);

        return await _kernel.ExecuteAsync(async ct =>
        {
            var token = await _db.RefreshTokens
                .SingleOrDefaultAsync(
                    x => x.TokenHash == hash &&
                         x.TenantId == tenantId,
                    ct);

            if (token is null)
                return RefreshTokenValidationResult<TUserId>.Invalid();

            if (token.RevokedAt != null)
                return RefreshTokenValidationResult<TUserId>.ReuseDetected();

            if (token.ExpiresAt <= now)
            {
                token.RevokedAt = now;
                return RefreshTokenValidationResult<TUserId>.Invalid();
            }

            // Revoke on first use (rotation)
            token.RevokedAt = now;

            var session = await _sessions.GetSessionAsync(
                tenantId,
                token.SessionId,
                ct);

            if (session is null ||
                session.IsRevoked ||
                session.ExpiresAt <= now)
            {
                return RefreshTokenValidationResult<TUserId>.Invalid();
            }

            return RefreshTokenValidationResult<TUserId>.Valid(
                session.UserId,
                session.SessionId);
        });
    }

    public Task RevokeRefreshTokenAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at)
    {
        return _kernel.ExecuteAsync(async ct =>
        {
            var tokens = await _db.RefreshTokens
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.SessionId == sessionId &&
                    x.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in tokens)
                token.RevokedAt = at;
        });
    }

    public Task RevokeAllRefreshTokensAsync(string? tenantId, TUserId _, DateTimeOffset at)
    {
        return _kernel.ExecuteAsync(async ct =>
        {
            var tokens = await _db.RefreshTokens
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in tokens)
                token.RevokedAt = at;
        });
    }

    // ------------------------------------------------------------
    // JWT ID (JTI)
    // ------------------------------------------------------------

    public Task StoreTokenIdAsync(string? tenantId, string jti, DateTimeOffset expiresAt)
    {
        return _kernel.ExecuteAsync(ct =>
        {
            _db.RevokedTokenIds.Add(new RevokedTokenIdProjection
            {
                TenantId = tenantId,
                Jti = jti,
                ExpiresAt = expiresAt,
                RevokedAt = expiresAt
            });

            return Task.CompletedTask;
        });
    }

    public async Task<bool> IsTokenIdRevokedAsync(string? tenantId, string jti)
    {
        return await _db.RevokedTokenIds
            .AsNoTracking()
            .AnyAsync(x =>
                x.Jti == jti &&
                x.TenantId == tenantId);
    }

    public Task RevokeTokenIdAsync(string? tenantId, string jti, DateTimeOffset at)
    {
        return _kernel.ExecuteAsync(async ct =>
        {
            var record = await _db.RevokedTokenIds
                .SingleOrDefaultAsync(
                    x => x.Jti == jti &&
                         x.TenantId == tenantId,
                    ct);

            if (record is null)
            {
                _db.RevokedTokenIds.Add(new RevokedTokenIdProjection
                {
                    TenantId = tenantId,
                    Jti = jti,
                    ExpiresAt = at,
                    RevokedAt = at
                });
            }
            else
            {
                record.RevokedAt = at;
            }
        });
    }
}
