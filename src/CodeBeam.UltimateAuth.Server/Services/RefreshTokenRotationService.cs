using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services;

public sealed class RefreshTokenRotationService : IRefreshTokenRotationService
{
    private readonly IRefreshTokenValidator _validator;
    private readonly IRefreshTokenStoreFactory _storeFactory;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;

    public RefreshTokenRotationService(IRefreshTokenValidator validator, IRefreshTokenStoreFactory storeFactory, ITokenIssuer tokenIssuer, IClock clock)
    {
        _validator = validator;
        _storeFactory = storeFactory;
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

        var store = _storeFactory.Create(validation.Tenant);

        if (validation.IsReuseDetected)
        {
            if (validation.ChainId is not null)
            {
                await store.RevokeByChainAsync(validation.ChainId.Value, context.Now, ct);
            }
            else if (validation.SessionId is not null)
            {
                await store.RevokeBySessionAsync(validation.SessionId.Value, context.Now, ct);
            }

            return new RefreshTokenRotationExecution() { Result = RefreshTokenRotationResult.Failed() };
        }

        if (validation.UserKey is not UserKey userKey)
            throw new UAuthValidationException("Validated refresh token does not contain a UserKey.");

        if (validation.SessionId is not AuthSessionId sessionId)
            throw new UAuthValidationException("Validated refresh token does not contain a SessionId.");

        if (validation.TokenHash == null)
            throw new UAuthValidationException("Validated refresh token does not contain a hashed token.");

        var tokenContext = new TokenIssuanceContext
        {
            Tenant = flow.OriginalOptions.MultiTenant.Enabled
                ? validation.Tenant
                : TenantKey.Single,

            UserKey = userKey,
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
        await store.ExecuteAsync(async ct2 =>
        {
            await store.RevokeAsync(validation.TokenHash, context.Now, refreshToken.TokenHash, ct2);

            var stored = RefreshToken.Create(
                tokenId: TokenId.New(),
                tokenHash: refreshToken.TokenHash,
                tenant: validation.Tenant,
                userKey: userKey,
                sessionId: sessionId,
                chainId: validation.ChainId,
                createdAt: _clock.UtcNow,
                expiresAt: refreshToken.ExpiresAt
            );

            await store.StoreAsync(stored, ct2);

        }, ct);


        return new RefreshTokenRotationExecution
        {
            Tenant = validation.Tenant,
            UserKey = validation.UserKey,
            SessionId = validation.SessionId,
            ChainId = validation.ChainId,
            Result = RefreshTokenRotationResult.Success(accessToken, refreshToken)
        };
    }
}
