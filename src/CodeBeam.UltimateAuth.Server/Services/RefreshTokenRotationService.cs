using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;
using System;

namespace CodeBeam.UltimateAuth.Server.Services;

public sealed class RefreshTokenRotationService : IRefreshTokenRotationService
{
    private readonly IRefreshTokenValidator _validator;
    private readonly IRefreshTokenStore _store;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;

    public RefreshTokenRotationService(IRefreshTokenValidator validator, IRefreshTokenStore store, ITokenIssuer tokenIssuer, IClock clock)
    {
        _validator = validator;
        _store = store;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
    }

    // TODO: Handle reuse detection and make flow knows situation, but don't make security branch.
    public async Task<RefreshTokenRotationExecution> RotateAsync(AuthFlowContext flow, RefreshTokenRotationContext context, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = flow.Tenant,
                RefreshToken = context.RefreshToken,
                Now = context.Now,
                Device = context.Device,
                ExpectedSessionId = context.ExpectedSessionId
            },
            ct);

        if (!validation.IsValid)
            return new RefreshTokenRotationExecution() { Result = RefreshTokenRotationResult.Failed() };

        if (validation.IsReuseDetected)
        {
            if (validation.ChainId is not null)
            {
                await _store.RevokeByChainAsync(validation.Tenant, validation.ChainId.Value, context.Now, ct);
            }
            else if (validation.SessionId is not null)
            {
                await _store.RevokeBySessionAsync(validation.Tenant, validation.SessionId.Value, context.Now, ct);
            }

            return new RefreshTokenRotationExecution() { Result = RefreshTokenRotationResult.Failed() };
        }

        if (validation.UserKey is not UserKey uKey)
        {
            throw new InvalidOperationException("Validated refresh token does not contain a UserKey.");
        }

        var tokenContext = new TokenIssuanceContext
        {
            Tenant = flow.OriginalOptions.MultiTenant.Enabled
                ? validation.Tenant
                : TenantKey.Single,

            UserKey = uKey,
            SessionId = validation.SessionId,
            ChainId = validation.ChainId
        };

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(flow, tokenContext, ct);
        var refreshToken = await _tokenIssuer.IssueRefreshTokenAsync(flow, tokenContext, RefreshTokenPersistence.DoNotPersist, ct);

        if (refreshToken is null)
            return new RefreshTokenRotationExecution
            {
                Result = RefreshTokenRotationResult.Failed()
            };

        // Never issue new refresh token before revoke old. Upperline doesn't persist token currently.
        // TODO: Add _store.ExecuteAsync here to wrap RevokeAsync and StoreAsync
        await _store.RevokeAsync(validation.Tenant, validation.TokenHash, context.Now, refreshToken.TokenHash, ct);

        var stored = new StoredRefreshToken
        {
            Tenant = flow.Tenant,
            TokenHash = refreshToken.TokenHash,
            UserKey = uKey,
            SessionId = validation.SessionId.Value,
            ChainId = validation.ChainId,
            IssuedAt = _clock.UtcNow,
            ExpiresAt = refreshToken.ExpiresAt
        };
        await _store.StoreAsync(validation.Tenant, stored);

        return new RefreshTokenRotationExecution()
        {
            Tenant = validation.Tenant,
            UserKey = validation.UserKey,
            SessionId = validation.SessionId,
            ChainId = validation.ChainId,
            Result = RefreshTokenRotationResult.Success(accessToken, refreshToken)
        };
    }
}
