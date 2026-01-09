using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class DefaultRefreshTokenValidator<TUserId> : IRefreshTokenValidator<TUserId>
{
    private readonly IRefreshTokenStore<TUserId> _store;
    private readonly ITokenHasher _hasher;

    public DefaultRefreshTokenValidator(IRefreshTokenStore<TUserId> store, ITokenHasher hasher)
    {
        _store = store;
        _hasher = hasher;
    }

    public async Task<RefreshTokenValidationResult<TUserId>> ValidateAsync(RefreshTokenValidationContext<TUserId> context, CancellationToken ct = default)
    {
        var hash = _hasher.Hash(context.RefreshToken);
        var stored = await _store.FindByHashAsync(context.TenantId, hash, ct);

        if (stored is null)
            return RefreshTokenValidationResult<TUserId>.Invalid();

        if (stored.IsRevoked)
            return RefreshTokenValidationResult<TUserId>.ReuseDetected(
                tenantId: stored.TenantId,
                sessionId: stored.SessionId,
                chainId: stored.ChainId,
                userId: stored.UserId);

        if (stored.IsExpired(context.Now))
        {
            await _store.RevokeAsync(context.TenantId, hash, context.Now, null, ct);
            return RefreshTokenValidationResult<TUserId>.Invalid();
        }

        if (context.ExpectedSessionId.HasValue && stored.SessionId != context.ExpectedSessionId)
        {
            return RefreshTokenValidationResult<TUserId>.Invalid();
        }

        // TODO: Add device binding
        // if (context.Device != null && !stored.MatchesDevice(context.Device))
        //     return Invalid();

        return RefreshTokenValidationResult<TUserId>.Valid(
            tenantId: stored.TenantId,
            stored.UserId,
            stored.SessionId,
            hash,
            stored.ChainId);
    }
}
