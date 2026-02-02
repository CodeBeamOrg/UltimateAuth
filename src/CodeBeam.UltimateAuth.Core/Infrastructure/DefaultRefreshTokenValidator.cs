using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class DefaultRefreshTokenValidator : IRefreshTokenValidator
{
    private readonly IRefreshTokenStore _store;
    private readonly ITokenHasher _hasher;

    public DefaultRefreshTokenValidator(IRefreshTokenStore store, ITokenHasher hasher)
    {
        _store = store;
        _hasher = hasher;
    }

    public async Task<RefreshTokenValidationResult> ValidateAsync(RefreshTokenValidationContext context, CancellationToken ct = default)
    {
        var hash = _hasher.Hash(context.RefreshToken);
        var stored = await _store.FindByHashAsync(context.Tenant, hash, ct);

        if (stored is null)
            return RefreshTokenValidationResult.Invalid();

        if (stored.IsRevoked)
            return RefreshTokenValidationResult.ReuseDetected(
                tenant: stored.Tenant,
                sessionId: stored.SessionId,
                chainId: stored.ChainId,
                userKey: stored.UserKey);

        if (stored.IsExpired(context.Now))
        {
            await _store.RevokeAsync(context.Tenant, hash, context.Now, null, ct);
            return RefreshTokenValidationResult.Invalid();
        }

        if (context.ExpectedSessionId.HasValue && stored.SessionId != context.ExpectedSessionId)
        {
            return RefreshTokenValidationResult.Invalid();
        }

        // TODO: Add device binding
        // if (context.Device != null && !stored.MatchesDevice(context.Device))
        //     return Invalid();

        return RefreshTokenValidationResult.Valid(
            tenant: stored.Tenant,
            stored.UserKey,
            stored.SessionId,
            hash,
            stored.ChainId);
    }
}
