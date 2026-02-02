using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tokens.InMemory;

public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private static string NormalizeTenant(string? tenantId) => tenantId ?? "__default__";

    private readonly ConcurrentDictionary<TokenKey, StoredRefreshToken> _tokens = new();

    public Task StoreAsync(TenantKey tenant, StoredRefreshToken token, CancellationToken ct = default)
    {
        var key = new TokenKey(NormalizeTenant(tenant), token.TokenHash);

        _tokens[key] = token;
        return Task.CompletedTask;
    }

    public Task<StoredRefreshToken?> FindByHashAsync(TenantKey tenant, string tokenHash, CancellationToken ct = default)
    {
        var key = new TokenKey(NormalizeTenant(tenant), tokenHash);

        _tokens.TryGetValue(key, out var token);
        return Task.FromResult(token);
    }

    public Task RevokeAsync(TenantKey tenant, string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken ct = default)
    {
        var key = new TokenKey(NormalizeTenant(tenant), tokenHash);

        if (_tokens.TryGetValue(key, out var token) && !token.IsRevoked)
        {
            _tokens[key] = token with
            {
                RevokedAt = revokedAt,
                ReplacedByTokenHash = replacedByTokenHash
            };
        }

        return Task.CompletedTask;
    }

    public Task RevokeBySessionAsync(TenantKey tenant, AuthSessionId sessionId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        foreach (var (key, token) in _tokens)
        {
            if (key.TenantId == tenant &&
                token.SessionId == sessionId &&
                !token.IsRevoked)
            {
                _tokens[key] = token with { RevokedAt = revokedAt };
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeByChainAsync(TenantKey tenant, SessionChainId chainId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        foreach (var (key, token) in _tokens)
        {
            if (key.TenantId == tenant &&
                token.ChainId == chainId &&
                !token.IsRevoked)
            {
                _tokens[key] = token with { RevokedAt = revokedAt };
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllForUserAsync(TenantKey tenant, UserKey userKey, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        foreach (var (key, token) in _tokens)
        {
            if (key.TenantId == tenant &&
                token.UserKey == userKey &&
                !token.IsRevoked)
            {
                _tokens[key] = token with { RevokedAt = revokedAt };
            }
        }

        return Task.CompletedTask;
    }

    private readonly record struct TokenKey(string TenantId, string TokenHash);
}
