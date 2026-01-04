using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services;

public sealed class RefreshTokenRotationService<TUserId> : IRefreshTokenRotationService<TUserId>
{
    private readonly IRefreshTokenValidator<TUserId> _validator;
    private readonly IRefreshTokenStore<TUserId> _store;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;

    public RefreshTokenRotationService(
        IRefreshTokenValidator<TUserId> validator,
        IRefreshTokenStore<TUserId> store,
        ITokenIssuer tokenIssuer,
        IClock clock)
    {
        _validator = validator;
        _store = store;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
    }

    public async Task<RefreshTokenRotationResult> RotateAsync(AuthFlowContext flow, RefreshTokenRotationContext<TUserId> context, CancellationToken ct = default)
    {
        var now = context.Now;
        var validation = await _validator.ValidateAsync(
            flow.TenantId,
            context.RefreshToken,
            now,
            ct);

        // ❌ Invalid
        if (!validation.IsValid)
            return RefreshTokenRotationResult.Failed();

        // 🚨 Reuse detected → nuke from orbit
        if (validation.IsReuseDetected)
        {
            if (validation.ChainId is not null)
            {
                await _store.RevokeByChainAsync(validation.TenantId, validation.ChainId.Value, now, ct);
            }
            else if (validation.SessionId is not null)
            {
                await _store.RevokeBySessionAsync(validation.TenantId, validation.SessionId.Value, now, ct);
            }

            return RefreshTokenRotationResult.Failed();
        }


        // ✅ Valid rotation
        var tokenContext = new TokenIssuanceContext
        {
            TenantId = flow.ServerOptions.Options.MultiTenant.Enabled
                ? validation.TenantId
                : null,

            UserId = validation.UserId!.ToString()!,
            SessionId = validation.SessionId!.Value
        };

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(flow, tokenContext, ct);
        var refreshToken = await _tokenIssuer.IssueRefreshTokenAsync(flow, tokenContext, ct);

        return RefreshTokenRotationResult.Success(accessToken, refreshToken!);
    }
}
