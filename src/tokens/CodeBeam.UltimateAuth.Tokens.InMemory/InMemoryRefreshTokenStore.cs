using System.Collections.Concurrent;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tokens.InMemory;

public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private static string NormalizeTenant(string? tenantId) => tenantId ?? "__default__";

    private readonly ConcurrentDictionary<TokenKey, StoredRefreshToken> _tokens = new();

    public Task StoreAsync(string? tenantId, StoredRefreshToken token, CancellationToken ct = default)
    {
        var key = new TokenKey(NormalizeTenant(tenantId), token.TokenHash);

        _tokens[key] = token;
        return Task.CompletedTask;
    }

    public Task<StoredRefreshToken?> FindByHashAsync(string? tenantId, string tokenHash, CancellationToken ct = default)
    {
        var key = new TokenKey(NormalizeTenant(tenantId), tokenHash);

        _tokens.TryGetValue(key, out var token);
        return Task.FromResult(token);
    }

    public Task RevokeAsync(string? tenantId, string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken ct = default)
    {
        var key = new TokenKey(NormalizeTenant(tenantId), tokenHash);

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

    public Task RevokeBySessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        var tenant = NormalizeTenant(tenantId);

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

    public Task RevokeByChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        var tenant = NormalizeTenant(tenantId);

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

    public Task RevokeAllForUserAsync(string? tenantId, UserKey userKey, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        var tenant = NormalizeTenant(tenantId);

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
