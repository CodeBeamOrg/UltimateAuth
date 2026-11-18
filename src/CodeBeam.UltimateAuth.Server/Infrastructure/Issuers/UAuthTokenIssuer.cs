using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

/// <summary>
/// Default UltimateAuth token issuer.
/// Opinionated implementation of ITokenIssuer.
/// Mode-aware (PureOpaque, Hybrid, SemiHybrid, PureJwt).
/// </summary>
public sealed class UAuthTokenIssuer : ITokenIssuer
{
    private readonly IOpaqueTokenGenerator _opaqueGenerator;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IRefreshTokenStoreFactory _storeFactory;
    private readonly IClock _clock;

    public UAuthTokenIssuer(IOpaqueTokenGenerator opaqueGenerator, IJwtTokenGenerator jwtGenerator, ITokenHasher tokenHasher, IRefreshTokenStoreFactory storeFactory, IClock clock)
    {
        _opaqueGenerator = opaqueGenerator;
        _jwtGenerator = jwtGenerator;
        _tokenHasher = tokenHasher;
        _storeFactory = storeFactory;
        _clock = clock;
    }

    public Task<AccessToken> IssueAccessTokenAsync(AuthFlowContext flow, TokenIssuanceContext context, CancellationToken ct = default)
    {
        var tokens = flow.Options.Token;
        var now = _clock.UtcNow;
        var expires = now.Add(tokens.AccessTokenLifetime);

        return flow.EffectiveMode switch
        {
            UAuthMode.PureOpaque or UAuthMode.Hybrid =>
                Task.FromResult(IssueOpaqueAccessToken(expires, context.SessionId)),

            UAuthMode.SemiHybrid =>
                Task.FromResult(IssueJwtAccessToken(context, tokens, expires)),

            UAuthMode.PureJwt
                when context.SessionId is null &&
                     context.ChainId is null &&
                     context.DeviceId is not null =>
                Task.FromResult(IssueJwtAccessToken(context, tokens, expires)),

            UAuthMode.PureJwt =>
                throw new InvalidOperationException("PureJwt requires a device ID and must not be bound to a session or chain."),

            _
                => throw new InvalidOperationException($"Unsupported auth mode: {flow.EffectiveMode}")
        };
    }

    public async Task<RefreshTokenIssuanceResult?> IssueRefreshTokenAsync(AuthFlowContext flow, TokenIssuanceContext context, RefreshTokenPersistence persistence, CancellationToken ct = default)
    {
        if (flow.EffectiveMode == UAuthMode.PureOpaque || !flow.Options.Token.IssueRefresh)
        {
            return null;
        }

        if (flow.EffectiveMode == UAuthMode.PureJwt)
        {
            if (context.SessionId is not null ||
                context.ChainId is not null ||
                context.DeviceId is null)
            {
                throw new InvalidOperationException("PureJwt refresh requires a device ID and must not be session-bound.");
            }
        }
        else if (context.SessionId is null)
        {
            return null;
        }

        var now = _clock.UtcNow;
        var expires = now.Add(flow.Options.Token.RefreshTokenLifetime);

        var raw = _opaqueGenerator.Generate();
        var hash = _tokenHasher.Hash(raw);

        var stored = RefreshToken.Create(
            tokenId: TokenId.New(),
            tokenHash: hash,
            tenant: flow.Tenant,
            userKey: context.UserKey,
            sessionId: context.SessionId,
            chainId: context.ChainId,
            createdAt: now,
            expiresAt: expires,
            refreshChainId: context.RefreshChainId,
            deviceId: context.DeviceId);

        if (persistence == RefreshTokenPersistence.Persist)
        {
            var store = _storeFactory.Create(flow.Tenant);

            await store.ExecuteAsync(
                async transactionCt =>
                {
                    await store.StoreAsync(stored, transactionCt);
                },
                ct);
        }

        return new RefreshTokenIssuanceResult
        {
            Token = raw,
            TokenHash = hash,
            ExpiresAt = expires
        };
    }

    private AccessToken IssueOpaqueAccessToken(DateTimeOffset expires, AuthSessionId? sessionId)
    {
        string token = _opaqueGenerator.Generate();

        return new AccessToken
        {
            Token = token,
            Format = TokenFormat.Opaque,
            ExpiresAt = expires,
            SessionId = sessionId?.Value
        };
    }

    private AccessToken IssueJwtAccessToken(TokenIssuanceContext context, UAuthTokenOptions tokens, DateTimeOffset expires)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal);

        // Custom/application claims are added first.
        // Framework-owned security claims below always take precedence.
        foreach (var (type, values) in context.Claims.Claims)
        {
            if (values.Count == 0)
                continue;

            // Framework - owned claims cannot be supplied by applications.
            if (type is
                UAuthJwtProfileClaims.PrimaryUserName or
                UAuthJwtProfileClaims.DisplayName or
                UAuthJwtProfileClaims.TimeZone or
                UAuthJwtProfileClaims.AuthenticatedAt or
                "sub" or
                "tenant" or
                "sid" or
                "device_id" or
                "jti" or
                "iss" or
                "aud" or
                "exp" or
                "nbf" or
                "iat")
                        continue;

            claims[type] = values.Count == 1
                ? values.First()
                : values.ToArray();
        }

        // UltimateAuth-owned identity/security claims must never be overridable
        // by caller-provided claims.
        claims["sub"] = context.UserKey.Value;
        claims["tenant"] = context.Tenant.Value;

        if (!string.IsNullOrWhiteSpace(context.PrimaryUserName))
        {
            claims[UAuthJwtProfileClaims.PrimaryUserName] = context.PrimaryUserName;
        }

        if (!string.IsNullOrWhiteSpace(context.DisplayName))
        {
            claims[UAuthJwtProfileClaims.DisplayName] = context.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(context.TimeZone))
        {
            claims[UAuthJwtProfileClaims.TimeZone] = context.TimeZone;
        }

        if (context.AuthenticatedAt is DateTimeOffset authenticatedAt)
        {
            claims[UAuthJwtProfileClaims.AuthenticatedAt] = authenticatedAt.ToUniversalTime().ToString("O");
        }

        if (context.DeviceId is DeviceId deviceId)
        {
            claims["device_id"] = deviceId.Value;
        }
        else if (context.SessionId is null)
        {
            throw new InvalidOperationException("Session-free JWT issuance requires a device ID.");
        }

        if (context.SessionId is AuthSessionId sessionId)
            claims["sid"] = sessionId.Value;
        else
            claims.Remove("sid");

        if (tokens.AddJwtIdClaim)
            claims["jti"] = _opaqueGenerator.GenerateJwtId();

        var descriptor = new UAuthJwtTokenDescriptor
        {
            Subject = context.UserKey,
            Issuer = tokens.Issuer,
            Audience = tokens.Audience,
            IssuedAt = _clock.UtcNow,
            ExpiresAt = expires,
            Tenant = context.Tenant,
            DeviceId = context.DeviceId,
            Claims = claims,
            KeyId = tokens.KeyId
        };

        var jwt = _jwtGenerator.CreateToken(descriptor);

        return new AccessToken
        {
            Token = jwt,
            Format = TokenFormat.Jwt,
            ExpiresAt = expires,
            SessionId = context.SessionId?.ToString()
        };
    }
}
