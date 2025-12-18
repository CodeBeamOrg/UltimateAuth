using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using System.Collections.Concurrent;

namespace CodeBeam.UltimateAuth.Tokens.InMemory;

internal sealed class InMemoryTokenStoreKernel : ITokenStoreKernel
{
    private readonly ConcurrentDictionary<string, StoredRefreshToken> _refreshTokens = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _revokedJtis = new();

    public Task SaveRefreshTokenAsync(string? _, StoredRefreshToken token)
    {
        _refreshTokens[token.TokenHash] = token;
        return Task.CompletedTask;
    }

    public Task<StoredRefreshToken?> GetRefreshTokenAsync(string? _, string tokenHash)
    {
        _refreshTokens.TryGetValue(tokenHash, out var token);
        return Task.FromResult(token);
    }

    public Task RevokeRefreshTokenAsync(string? _, string tokenHash, DateTimeOffset at)
    {
        if (_refreshTokens.TryGetValue(tokenHash, out var token))
        {
            _refreshTokens[tokenHash] = token with { RevokedAt = at };
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllRefreshTokensAsync(string? _, string? __, DateTimeOffset at)
    {
        foreach (var kvp in _refreshTokens)
        {
            _refreshTokens[kvp.Key] = kvp.Value with { RevokedAt = at };
        }

        return Task.CompletedTask;
    }

    public Task DeleteExpiredRefreshTokensAsync(string? _, DateTimeOffset now)
    {
        var dict = (IDictionary<string, StoredRefreshToken>)_refreshTokens;

        foreach (var kvp in dict.ToList())
        {
            if (kvp.Value.ExpiresAt <= now)
            {
                dict.Remove(kvp.Key);
            }
        }

        return Task.CompletedTask;
    }

    // ------------------------------------------------------------
    // JWT ID (JTI)
    // ------------------------------------------------------------

    public Task StoreTokenIdAsync(string? _, string jti, DateTimeOffset expiresAt)
    {
        _revokedJtis[jti] = expiresAt;
        return Task.CompletedTask;
    }

    public Task<bool> IsTokenIdRevokedAsync(string? _, string jti)
        => Task.FromResult(_revokedJtis.ContainsKey(jti));

    public Task RevokeTokenIdAsync(string? _, string jti, DateTimeOffset at)
    {
        _revokedJtis[jti] = at;
        return Task.CompletedTask;
    }
}
