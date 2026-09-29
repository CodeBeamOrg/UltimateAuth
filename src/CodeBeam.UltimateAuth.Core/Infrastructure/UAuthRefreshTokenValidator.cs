using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class UAuthRefreshTokenValidator : IRefreshTokenValidator
{
    private readonly IRefreshTokenStoreFactory _storeFactory;
    private readonly ITokenHasher _hasher;

    public UAuthRefreshTokenValidator(IRefreshTokenStoreFactory storeFactory, ITokenHasher hasher)
    {
        _storeFactory = storeFactory;
        _hasher = hasher;
    }

    public async Task<RefreshTokenValidationResult> ValidateAsync(RefreshTokenValidationContext context, CancellationToken ct = default)
    {
        var store = _storeFactory.Create(context.Tenant);

        var hash = _hasher.Hash(context.RefreshToken);

        var stored = await store.FindByHashAsync(hash, ct);

        if (stored is null)
            return RefreshTokenValidationResult.NotFound();

        if (stored.IsExpired(context.Now))
            return RefreshTokenValidationResult.Expired(stored);

        if (context.ExpectedSessionId.HasValue &&
            stored.SessionId != context.ExpectedSessionId.Value)
        {
            return RefreshTokenValidationResult.Invalid();
        }

        if (stored.IsRevoked)
        {
            if (stored.ReplacedByTokenHash is not null)
            {
                return RefreshTokenValidationResult.Consumed(stored);
            }

            return RefreshTokenValidationResult.Invalid();
        }

        return RefreshTokenValidationResult.Valid(stored, hash);
    }
}
