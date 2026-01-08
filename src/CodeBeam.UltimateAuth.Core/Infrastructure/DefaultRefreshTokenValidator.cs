using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class DefaultRefreshTokenValidator<TUserId> : IRefreshTokenValidator<TUserId>
{
    private readonly IRefreshTokenStore<TUserId> _store;
    private readonly ITokenHasher _hasher;

    public DefaultRefreshTokenValidator(
        IRefreshTokenStore<TUserId> store,
        ITokenHasher hasher)
    {
        _store = store;
        _hasher = hasher;
    }

    public async Task<RefreshTokenValidationResult<TUserId>> ValidateAsync(
        string? tenantId,
        string refreshToken,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var hash = _hasher.Hash(refreshToken);

        var stored = await _store.FindByHashAsync(tenantId, hash, ct);

        if (stored is null)
            return RefreshTokenValidationResult<TUserId>.Invalid();

        if (stored.IsRevoked)
            return RefreshTokenValidationResult<TUserId>.ReuseDetected(
                tenantId: stored.TenantId,
                sessionId: stored.SessionId,
                chainId: stored.ChainId,
                userId: stored.UserId);

        if (stored.IsExpired(now))
        {
            await _store.RevokeAsync(tenantId, hash, now, ct);
            return RefreshTokenValidationResult<TUserId>.Invalid();
        }

        return RefreshTokenValidationResult<TUserId>.Valid(
            tenantId: stored.TenantId,
            stored.UserId,
            stored.SessionId,
            stored.ChainId);
    }
}
