using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Services;

public sealed class RefreshTokenRotationService : IRefreshTokenRotationService
{
    private readonly IRefreshTokenValidator _validator;
    private readonly IRefreshTokenStoreFactory _storeFactory;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IUserClaimsProvider _claimsProvider;


    public RefreshTokenRotationService(IRefreshTokenValidator validator, IRefreshTokenStoreFactory storeFactory, ITokenIssuer tokenIssuer, IUserClaimsProvider claimsProvider)
    {
        _validator = validator;
        _storeFactory = storeFactory;
        _tokenIssuer = tokenIssuer;
        _claimsProvider = claimsProvider;
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

        if (validation.State == RefreshTokenValidationState.Consumed)
        {
            var concurrencyWindow = flow.OriginalOptions.Token.RefreshTokenConcurrentRequestWindow;

            if (IsLikelyConcurrentRequest(validation, context.Now, concurrencyWindow))
            {
                return new RefreshTokenRotationExecution
                {
                    Result = RefreshTokenRotationResult.Failed()
                };
            }

            await HandleReplayAsync(validation, context.Now, ct);

            return new RefreshTokenRotationExecution
            {
                Result = RefreshTokenRotationResult.Failed()
            };
        }

        if (!validation.IsValid)
        {
            return new RefreshTokenRotationExecution { Result = RefreshTokenRotationResult.Failed() };
        }

        if (validation.UserKey is not UserKey userKey)
            throw new UAuthValidationException("Validated refresh token does not contain a UserKey.");

        if (validation.SessionId is not AuthSessionId sessionId)
            throw new UAuthValidationException("Validated refresh token does not contain a SessionId.");

        if (validation.TokenHash is null)
            throw new UAuthValidationException("Validated refresh token does not contain a hashed token.");

        var store = _storeFactory.Create(validation.Tenant);

        var claims = await _claimsProvider.GetClaimsAsync(validation.Tenant, userKey, ct);

        var tokenContext = new TokenIssuanceContext
        {
            Tenant = flow.OriginalOptions.MultiTenant.Enabled
                ? validation.Tenant
                : TenantKey.Single,

            UserKey = userKey,
            SessionId = sessionId,
            ChainId = validation.ChainId,
            Claims = claims
        };

        // Generate candidate replacement refresh token.
        // Do not persist it yet.
        var refreshToken = await _tokenIssuer.IssueRefreshTokenAsync(flow, tokenContext, RefreshTokenPersistence.DoNotPersist, ct);

        if (refreshToken is null)
        {
            return new RefreshTokenRotationExecution { Result = RefreshTokenRotationResult.Failed() };
        }

        // Only one concurrent request is allowed to consume
        // the current refresh token.
        var consumed = await store.ExecuteAsync(
            async ct2 =>
            {
                var acquired = await store.TryConsumeAsync(validation.TokenHash, context.Now, refreshToken.TokenHash, ct2);

                if (!acquired)
                    return false;

                var stored = RefreshToken.Create(
                    tokenId: TokenId.New(),
                    tokenHash: refreshToken.TokenHash,
                    tenant: validation.Tenant,
                    userKey: userKey,
                    sessionId: sessionId,
                    chainId: validation.ChainId,
                    createdAt: context.Now,
                    expiresAt: refreshToken.ExpiresAt);

                await store.StoreAsync(stored, ct2);

                return true;
            },
            ct);

        // Another concurrent request consumed this token first.
        if (!consumed)
        {
            return new RefreshTokenRotationExecution { Result = RefreshTokenRotationResult.Failed() };
        }

        // Only the winning request needs an access token.
        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(flow, tokenContext, ct);
        var refreshTokenInfo = new RefreshTokenInfo
        {
            Token = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt
        };

        return new RefreshTokenRotationExecution
        {
            Tenant = validation.Tenant,
            UserKey = userKey,
            SessionId = sessionId,
            ChainId = validation.ChainId,
            Result = RefreshTokenRotationResult.Success(accessToken, refreshTokenInfo)
        };
    }

    private async Task HandleReplayAsync(RefreshTokenValidationResult validation, DateTimeOffset now, CancellationToken ct)
    {
        var store = _storeFactory.Create(validation.Tenant);

        await store.ExecuteAsync(
            async ct2 =>
            {
                if (validation.ChainId is SessionChainId chainId)
                {
                    await store.RevokeByChainAsync(chainId, now, ct2);

                    return;
                }

                if (validation.SessionId is AuthSessionId sessionId)
                {
                    await store.RevokeBySessionAsync(sessionId, now, ct2);
                }
            },
            ct);
    }

    private static bool IsLikelyConcurrentRequest(RefreshTokenValidationResult validation, DateTimeOffset now, TimeSpan window)
    {
        if (validation.State != RefreshTokenValidationState.Consumed)
            return false;

        if (validation.ConsumedAt is not DateTimeOffset consumedAt)
            return false;

        if (validation.ReplacedByTokenHash is null)
            return false;

        var elapsed = now - consumedAt;

        return elapsed >= TimeSpan.Zero &&
               elapsed <= window;
    }
}
