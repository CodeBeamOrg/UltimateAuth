using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;
using System;

namespace CodeBeam.UltimateAuth.Server.Services;

public sealed class RefreshTokenRotationService<TUserId> : IRefreshTokenRotationService<TUserId>
{
    private readonly IRefreshTokenValidator<TUserId> _validator;
    private readonly IRefreshTokenStore<TUserId> _store;
    private readonly ITokenIssuer<TUserId> _tokenIssuer;
    private readonly IClock _clock;

    public RefreshTokenRotationService(
        IRefreshTokenValidator<TUserId> validator,
        IRefreshTokenStore<TUserId> store,
        ITokenIssuer<TUserId> tokenIssuer,
        IClock clock)
    {
        _validator = validator;
        _store = store;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
    }

    // TODO: Handle reuse detection and make flow knows situation, but don't make security branch.
    public async Task<RefreshTokenRotationExecution<TUserId>> RotateAsync(AuthFlowContext flow, RefreshTokenRotationContext<TUserId> context, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(
            new RefreshTokenValidationContext<TUserId>
            {
                TenantId = flow.TenantId,
                RefreshToken = context.RefreshToken,
                Now = context.Now,
                Device = context.Device,
                ExpectedSessionId = context.ExpectedSessionId
            },
            ct);

        if (!validation.IsValid)
            return new RefreshTokenRotationExecution<TUserId>() { Result = RefreshTokenRotationResult.Failed() };

        if (validation.IsReuseDetected)
        {
            if (validation.ChainId is not null)
            {
                await _store.RevokeByChainAsync(validation.TenantId, validation.ChainId.Value, context.Now, ct);
            }
            else if (validation.SessionId is not null)
            {
                await _store.RevokeBySessionAsync(validation.TenantId, validation.SessionId.Value, context.Now, ct);
            }

            return new RefreshTokenRotationExecution<TUserId>() { Result = RefreshTokenRotationResult.Failed() };
        }

        var tokenContext = new TokenIssuanceContext
        {
            TenantId = flow.OriginalOptions.MultiTenant.Enabled
                ? validation.TenantId
                : null,

            UserId = validation.UserId!.ToString()!,
            SessionId = validation.SessionId!.Value,
            ChainId = validation.ChainId
        };

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(flow, tokenContext, ct);
        var refreshToken = await _tokenIssuer.IssueRefreshTokenAsync(flow, tokenContext, RefreshTokenPersistence.DoNotPersist, ct);

        if (refreshToken is null)
            return new RefreshTokenRotationExecution<TUserId>
            {
                Result = RefreshTokenRotationResult.Failed()
            };

        // Never issue new refresh token before revoke old. Upperline doesn't persist token currently.
        // TODO: Add _store.ExecuteAsync here to wrap RevokeAsync and StoreAsync
        await _store.RevokeAsync(validation.TenantId, validation.TokenHash, context.Now, refreshToken.TokenHash, ct);

        var stored = new StoredRefreshToken<TUserId>
        {
            TenantId = flow.TenantId,
            TokenHash = refreshToken.TokenHash,
            UserId = validation.UserId,
            SessionId = validation.SessionId.Value,
            ChainId = validation.ChainId,
            IssuedAt = _clock.UtcNow,
            ExpiresAt = refreshToken.ExpiresAt
        };
        await _store.StoreAsync(validation.TenantId, stored);

        return new RefreshTokenRotationExecution<TUserId>()
        {
            TenantId = validation.TenantId,
            UserId = validation.UserId,
            SessionId = validation.SessionId,
            ChainId = validation.ChainId,
            Result = RefreshTokenRotationResult.Success(accessToken, refreshToken)
        };
    }
}
