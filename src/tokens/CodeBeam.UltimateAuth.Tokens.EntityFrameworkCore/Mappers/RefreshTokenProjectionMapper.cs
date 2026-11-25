using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

internal static class RefreshTokenProjectionMapper
{
    public static RefreshToken ToDomain(this RefreshTokenProjection p)
    {
        return RefreshToken.Create(
            tokenId: p.TokenId,
            tokenHash: p.TokenHash,
            tenant: p.Tenant,
            userKey: p.UserKey,
            sessionId: p.SessionId,
            sessionChainId: p.SessionChainId,
            createdAt: p.CreatedAt,
            authenticatedAt: p.AuthenticatedAt,
            expiresAt: p.ExpiresAt,
            refreshChainId: RefreshChainId.From(p.RefreshChainId),
            deviceId: p.DeviceId
        ) with
        {
            RevokedAt = p.RevokedAt,
            ReplacedByTokenHash = p.ReplacedByTokenHash,
            Version = p.Version
        };
    }

    public static RefreshTokenProjection ToProjection(this RefreshToken t)
    {
        return new RefreshTokenProjection
        {
            TokenId = t.TokenId,
            Tenant = t.Tenant,
            TokenHash = t.TokenHash,
            UserKey = t.UserKey,
            SessionId = t.SessionId,
            SessionChainId = t.SessionChainId,
            RefreshChainId = t.RefreshChainId.Value,
            DeviceId = t.DeviceId,
            CreatedAt = t.CreatedAt,
            AuthenticatedAt = t.AuthenticatedAt,
            ExpiresAt = t.ExpiresAt,
            RevokedAt = t.RevokedAt,
            ReplacedByTokenHash = t.ReplacedByTokenHash,
            Version = t.Version
        };
    }
}
