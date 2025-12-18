using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tokens.InMemory;

internal sealed class InMemoryTokenStore<TUserId> : ITokenStore<TUserId>
{
    private readonly ITokenStoreFactory _factory;
    private readonly ISessionStoreFactory _sessions;
    private readonly ITokenHasher _hasher;

    public InMemoryTokenStore(
        ITokenStoreFactory factory,
        ISessionStoreFactory sessions,
        ITokenHasher hasher)
    {
        _factory = factory;
        _sessions = sessions;
        _hasher = hasher;
    }

    public async Task StoreRefreshTokenAsync(
        string? tenantId,
        TUserId userId,
        AuthSessionId sessionId,
        string refreshTokenHash,
        DateTimeOffset expiresAt)
    {
        var kernel = _factory.Create(tenantId);

        var stored = new StoredRefreshToken
        {
            TokenHash = refreshTokenHash,
            SessionId = sessionId,
            ExpiresAt = expiresAt
        };

        await kernel.SaveRefreshTokenAsync(tenantId, stored);
    }

    public async Task<RefreshTokenValidationResult<TUserId>> ValidateRefreshTokenAsync(string? tenantId, string providedRefreshToken, DateTimeOffset now)
    {
        var kernel = _factory.Create(tenantId);

        var hash = _hasher.Hash(providedRefreshToken);
        var stored = await kernel.GetRefreshTokenAsync(tenantId, hash);

        if (stored is null)
            return RefreshTokenValidationResult<TUserId>.Invalid();

        if (stored.IsRevoked)
            return RefreshTokenValidationResult<TUserId>.ReuseDetected();

        if (stored.ExpiresAt <= now)
        {
            await kernel.RevokeRefreshTokenAsync(tenantId, hash, now);
            return RefreshTokenValidationResult<TUserId>.Invalid();
        }

        // one-time use
        await kernel.RevokeRefreshTokenAsync(tenantId, hash, now);

        var sessionKernel = _sessions.Create<TUserId>(tenantId);
        var session = await sessionKernel.GetSessionAsync(tenantId, stored.SessionId);

        if (session is null || session.IsRevoked || session.ExpiresAt <= now)
            return RefreshTokenValidationResult<TUserId>.Invalid();

        return RefreshTokenValidationResult<TUserId>.Valid(
            session.UserId,
            session.SessionId);
    }

    public Task RevokeRefreshTokenAsync(
        string? tenantId,
        AuthSessionId sessionId,
        DateTimeOffset at)
    {
        var kernel = _factory.Create(tenantId);
        return kernel.RevokeAllRefreshTokensAsync(tenantId, null, at);
    }

    public Task RevokeAllRefreshTokensAsync(
        string? tenantId,
        TUserId _,
        DateTimeOffset at)
    {
        var kernel = _factory.Create(tenantId);
        return kernel.RevokeAllRefreshTokensAsync(tenantId, null, at);
    }

    // ------------------------------------------------------------
    // JTI
    // ------------------------------------------------------------

    public Task StoreTokenIdAsync(
        string? tenantId,
        string jti,
        DateTimeOffset expiresAt)
    {
        var kernel = _factory.Create(tenantId);
        return kernel.StoreTokenIdAsync(tenantId, jti, expiresAt);
    }

    public Task<bool> IsTokenIdRevokedAsync(
        string? tenantId,
        string jti)
    {
        var kernel = _factory.Create(tenantId);
        return kernel.IsTokenIdRevokedAsync(tenantId, jti);
    }

    public Task RevokeTokenIdAsync(
        string? tenantId,
        string jti,
        DateTimeOffset at)
    {
        var kernel = _factory.Create(tenantId);
        return kernel.RevokeTokenIdAsync(tenantId, jti, at);
    }
}
