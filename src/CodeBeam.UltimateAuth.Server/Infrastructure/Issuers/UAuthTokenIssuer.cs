using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstactions;
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
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IClock _clock;

    public UAuthTokenIssuer(IOpaqueTokenGenerator opaqueGenerator, IJwtTokenGenerator jwtGenerator, ITokenHasher tokenHasher, IRefreshTokenStore refreshTokenStore, IClock clock)
    {
        _opaqueGenerator = opaqueGenerator;
        _jwtGenerator = jwtGenerator;
        _tokenHasher = tokenHasher;
        _refreshTokenStore = refreshTokenStore;
        _clock = clock;
    }

    public Task<AccessToken> IssueAccessTokenAsync(AuthFlowContext flow, TokenIssuanceContext context, CancellationToken ct = default)
    {
        var tokens = flow.OriginalOptions.Token;
        var now = _clock.UtcNow;
        var expires = now.Add(tokens.AccessTokenLifetime);

        return flow.EffectiveMode switch
        {
            // TODO: Discuss, Hybrid token may be JWT.
            UAuthMode.PureOpaque or UAuthMode.Hybrid =>
                Task.FromResult(IssueOpaqueAccessToken(expires, flow?.Session?.SessionId.ToString())),

            UAuthMode.SemiHybrid or
            UAuthMode.PureJwt =>
                Task.FromResult(IssueJwtAccessToken(context, tokens, expires)),

            _ => throw new InvalidOperationException($"Unsupported auth mode: {flow.EffectiveMode}")
        };
    }

    public async Task<RefreshToken?> IssueRefreshTokenAsync(AuthFlowContext flow, TokenIssuanceContext context, RefreshTokenPersistence persistence, CancellationToken ct = default)
    {
        if (flow.EffectiveMode == UAuthMode.PureOpaque)
            return null;

        var expires = _clock.UtcNow.Add(flow.OriginalOptions.Token.RefreshTokenLifetime);

        var raw = _opaqueGenerator.Generate();
        var hash = _tokenHasher.Hash(raw);

        if (context.SessionId is not AuthSessionId sessionId)
            return null;

        var stored = new StoredRefreshToken
        {
            Tenant = flow.Tenant,
            TokenHash = hash,
            UserKey = context.UserKey,
            SessionId = sessionId,
            ChainId = context.ChainId,
            IssuedAt = _clock.UtcNow,
            ExpiresAt = expires
        };

        if (persistence == RefreshTokenPersistence.Persist)
        {
            await _refreshTokenStore.StoreAsync(flow.Tenant, stored, ct);
        }

        return new RefreshToken
        {
            Token = raw,
            TokenHash = hash,
            ExpiresAt = expires
        };
    }

    private AccessToken IssueOpaqueAccessToken(DateTimeOffset expires, string? sessionId)
    {
        string token = _opaqueGenerator.Generate();

        return new AccessToken
        {
            Token = token,
            Type = TokenType.Opaque,
            ExpiresAt = expires,
            SessionId = sessionId
        };
    }

    private AccessToken IssueJwtAccessToken(TokenIssuanceContext context, UAuthTokenOptions tokens, DateTimeOffset expires)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = context.UserKey.Value,
            ["tenant"] = context.Tenant
        };

        foreach (var kv in context.Claims)
            claims[kv.Key] = kv.Value;

        if (context.SessionId != null)
            claims["sid"] = context.SessionId!;

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
            Claims = claims,
            KeyId = tokens.KeyId
        };

        var jwt = _jwtGenerator.CreateToken(descriptor);

        return new AccessToken
        {
            Token = jwt,
            Type = TokenType.Jwt,
            ExpiresAt = expires,
            SessionId = context.SessionId.ToString()
        };
    }
}
