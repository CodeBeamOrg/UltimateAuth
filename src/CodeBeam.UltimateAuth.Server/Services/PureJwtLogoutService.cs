using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services;

public sealed class PureJwtLogoutService : IPureJwtLogoutService
{
    private readonly IRefreshTokenValidator _validator;
    private readonly IRefreshTokenStoreFactory _storeFactory;
    private readonly IClock _clock;

    public PureJwtLogoutService(
        IRefreshTokenValidator validator,
        IRefreshTokenStoreFactory storeFactory,
        IClock clock)
    {
        _validator = validator;
        _storeFactory = storeFactory;
        _clock = clock;
    }

    public async Task<bool> LogoutAsync(AuthFlowContext flow, string refreshToken, CancellationToken ct = default)
    {
        if (flow.EffectiveMode != UAuthMode.PureJwt ||
            !flow.IsAuthenticated ||
            flow.UserKey is not UserKey userKey ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var now = _clock.UtcNow;

        var validation = await _validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = flow.Tenant,
                RefreshToken = refreshToken,
                Now = now,
                Device = flow.Device
            },
            ct);

        if (!validation.IsValid ||
            validation.Tenant != flow.Tenant ||
            validation.UserKey != userKey ||
            validation.SessionId is not null ||
            validation.SessionChainId is not null ||
            validation.DeviceId is null ||
            validation.RefreshChainId is not { } refreshChainId ||
            refreshChainId.Value == Guid.Empty)
        {
            return false;
        }

        var store = _storeFactory.Create(flow.Tenant);

        await store.ExecuteAsync(
            transactionCt =>
                store.RevokeByRefreshChainAsync(refreshChainId, now, transactionCt),
            ct);

        return true;
    }
}
