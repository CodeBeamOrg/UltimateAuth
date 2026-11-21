using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Services;

public sealed class RefreshTokenRotationService : IRefreshTokenRotationService
{
    private readonly IRefreshTokenValidator _validator;
    private readonly IRefreshTokenStoreFactory _storeFactory;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly IPrimaryUserIdentifierProvider _primaryIdentifiers;
    private readonly IUserProfileSnapshotProvider _userProfiles;


    public RefreshTokenRotationService(IRefreshTokenValidator validator, IRefreshTokenStoreFactory storeFactory, ITokenIssuer tokenIssuer, IUserClaimsProvider claimsProvider, IPrimaryUserIdentifierProvider primaryIdentifiers, IUserProfileSnapshotProvider userProfiles)
    {
        _validator = validator;
        _storeFactory = storeFactory;
        _tokenIssuer = tokenIssuer;
        _claimsProvider = claimsProvider;
        _primaryIdentifiers = primaryIdentifiers;
        _userProfiles = userProfiles;
    }

    // TODO: Handle reuse detection and make flow knows situation, but don't make security branch.
    public async Task<RefreshTokenRotationExecution> RotateAsync(AuthFlowContext flow, RefreshTokenRotationContext context, CancellationToken ct = default)
    {
        if (flow.EffectiveMode == UAuthMode.PureOpaque || !flow.Options.Token.IssueRefresh)
        {
            return Failed();
        }

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
            var window = flow.Options.Token.RefreshTokenConcurrentRequestWindow;

            if (!IsLikelyConcurrentRequest(validation, context.Now, window))
            {
                await HandleReplayAsync(validation, context.Now, ct);
            }

            return Failed();
        }

        if (!validation.IsValid ||
            validation.UserKey is not UserKey userKey ||
            string.IsNullOrWhiteSpace(validation.TokenHash))
        {
            return Failed();
        }

        var isPureJwt = flow.EffectiveMode == UAuthMode.PureJwt;

        // Enforce mode-specific refresh token structure.
        if (isPureJwt)
        {
            if (validation.SessionId is not null ||
                validation.SessionChainId is not null ||
                validation.DeviceId is null)
            {
                return Failed();
            }
        }
        else if (validation.SessionId is null)
        {
            return Failed();
        }

        if (validation.RefreshChainId is not { } refreshChainId || refreshChainId.Value == Guid.Empty)
            return Failed();

        var store = _storeFactory.Create(validation.Tenant);

        var claims = await _claimsProvider.GetClaimsAsync(validation.Tenant, userKey, ct);

        var identifiers = await _primaryIdentifiers.GetAsync(validation.Tenant, userKey, ct);

        var profile = await _userProfiles.GetAsync(validation.Tenant, userKey, ProfileKey.Default, ct);

        var tokenContext = new TokenIssuanceContext
        {
            Tenant = flow.Options.MultiTenant.Enabled
                ? validation.Tenant
                : TenantKey.Single,

            UserKey = userKey,
            SessionId = validation.SessionId,
            SessionChainId = validation.SessionChainId,
            RefreshChainId = validation.RefreshChainId,
            DeviceId = validation.DeviceId,
            Claims = claims,

            PrimaryUserName = identifiers?.UserName,
            DisplayName = profile?.DisplayName,
            TimeZone = profile?.TimeZone,
            AuthenticatedAt = validation.AuthenticatedAt
        };

        // Generate a replacement candidate without persisting it.
        var refreshToken = await _tokenIssuer.IssueRefreshTokenAsync(flow, tokenContext, RefreshTokenPersistence.DoNotPersist, ct);

        if (refreshToken is null)
            return Failed();

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(flow, tokenContext, ct);

        // Consume the old token and persist its replacement
        // within the same store execution scope.
        var consumed = await store.ExecuteAsync(
            async transactionCt =>
            {
                var acquired = await store.TryConsumeAsync(validation.TokenHash, context.Now, refreshToken.TokenHash, transactionCt);

                if (!acquired)
                    return false;

                var replacement = RefreshToken.Create(
                    tokenId: TokenId.New(),
                    tokenHash: refreshToken.TokenHash,
                    tenant: validation.Tenant,
                    userKey: userKey,
                    sessionId: validation.SessionId,
                    sessionChainId: validation.SessionChainId,
                    createdAt: context.Now,
                    authenticatedAt: validation.AuthenticatedAt,
                    expiresAt: refreshToken.ExpiresAt,
                    refreshChainId: validation.RefreshChainId,
                    deviceId: validation.DeviceId);

                await store.StoreAsync(replacement, transactionCt);

                return true;
            },
            ct);

        if (!consumed)
            return Failed();

        return new RefreshTokenRotationExecution
        {
            Tenant = validation.Tenant,
            UserKey = userKey,
            SessionId = validation.SessionId,
            SessionChainId = validation.SessionChainId,

            Result = RefreshTokenRotationResult.Success(
                accessToken,
                new RefreshTokenInfo
                {
                    Token = refreshToken.Token,
                    ExpiresAt = refreshToken.ExpiresAt
                })
        };
    }

    private static RefreshTokenRotationExecution Failed()
        => new() { Result = RefreshTokenRotationResult.Failed() };

    private async Task HandleReplayAsync(RefreshTokenValidationResult validation, DateTimeOffset now, CancellationToken ct)
    {
        var store = _storeFactory.Create(validation.Tenant);

        await store.ExecuteAsync(
            async transactionCt =>
            {
                if (validation.SessionChainId is SessionChainId sessionChainId)
                {
                    await store.RevokeBySessionChainAsync(sessionChainId, now, transactionCt);

                    return;
                }

                if (validation.SessionId is AuthSessionId sessionId)
                {
                    await store.RevokeBySessionAsync(sessionId, now, transactionCt);

                    return;
                }

                if (validation.RefreshChainId is { } refreshChainId && refreshChainId.Value != Guid.Empty)
                {
                    await store.RevokeByRefreshChainAsync(refreshChainId, now, transactionCt);
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

        return elapsed >= TimeSpan.Zero && elapsed <= window;
    }
}
